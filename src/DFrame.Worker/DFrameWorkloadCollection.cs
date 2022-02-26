#nullable enable

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace DFrame
{
    internal class DFrameWorkloadCollection
    {
        readonly Dictionary<string, DFrameWorkloadTypeInfo> dframeTypes;

        public static DFrameWorkloadCollection FromAssemblies(Assembly[] searchAssemblies, bool includesDefault, IServiceProviderIsService isService)
        {
            return new DFrameWorkloadCollection(searchAssemblies, includesDefault, isService);
        }

        DFrameWorkloadCollection(Assembly[] searchAssemblies, bool includesDefault, IServiceProviderIsService isService)
        {
            dframeTypes = new Dictionary<string, DFrameWorkloadTypeInfo>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var asm in searchAssemblies)
            {
                if (asm.FullName!.StartsWith("System") || asm.FullName.StartsWith("Microsoft.Extensions")) continue;

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(x => x != null).ToArray()!;
                }

                foreach (var workload in types)
                {
                    if (typeof(Workload).IsAssignableFrom(workload) && !workload.IsAbstract)
                    {
                        var isDefault = workload.GetCustomAttribute<DefaultWorkloadAttribute>(true);
                        if (isDefault != null)
                        {
                            if (!includesDefault) continue;
                        }

                        var attr = workload.GetCustomAttribute<WorkloadAttribute>(false);
                        var name = attr?.Name ?? workload.Name;

                        var t = new DFrameWorkloadTypeInfo(workload, name, isService);
                        if (!dframeTypes.TryAdd(name, t))
                        {
                            throw new InvalidOperationException($"Worker name is duplicate. name:{name}, type:{t.WorkloadType.FullName}");
                        }
                    }
                }
            }
        }

        public IEnumerable<DFrameWorkloadTypeInfo> All => dframeTypes.Values;

        public bool TryGetWorkload(string workloadName, [NotNullWhen(true)] out DFrameWorkloadTypeInfo? workload)
        {
            return dframeTypes.TryGetValue(workloadName, out workload);
        }
    }

    internal class DFrameWorkloadTypeInfo
    {
        readonly IServiceProviderIsService isService;

        public Type WorkloadType { get; }
        public string Name { get; }
        public WorkloadInfo WorkloadInfo { get; }
        public Lazy<ObjectFactory> Activator { get; }

        public DFrameWorkloadTypeInfo(Type workloadType, string name, IServiceProviderIsService isService)
        {
            this.isService = isService;
            this.WorkloadType = workloadType;
            this.Name = name;
            this.WorkloadInfo = CreateWorkloadInfo(workloadType, name, isService, out var activator);
            this.Activator = activator;
        }

        public object?[] CrateArgument(KeyValuePair<string, string>[] arguments)
        {
            var ctors = WorkloadType.GetConstructors();
            if (ctors.Length == 0) return Array.Empty<object>();

            var ctor = ctors[0];

            var dict = arguments.ToDictionary(x => x.Key, x => x.Value);

            var parameters = ctor.GetParameters();
            var result = parameters
                .Where(x => !isService.IsService(x.ParameterType))
                .Select(p =>
                {
                    if (!dict.TryGetValue(p.Name!, out string? parameterValue))
                    {
                        parameterValue = null;
                    }

                    var elementType = p.ParameterType;
                    var parameterType = ConvertToAllowParameterType(ref elementType, out var isNullable, out var isArray)!.Value;

                    // case of T[]
                    if (isArray)
                    {
                        if (string.IsNullOrEmpty(parameterValue)) return Array.CreateInstance(elementType, 0);

                        var values = parameterValue!.Split(',');
                        var array = Array.CreateInstance(elementType, values.Length);
                        for (int i = 0; i < array.Length; i++)
                        {
                            array.SetValue(Parse(values[i]), i);
                        }
                        return array;
                    }
                    else
                    {
                        return Parse(parameterValue);
                    }

                    object? Parse(string? v)
                    {
                        if (v == null || (elementType != typeof(string) && (v == "null") || v == ""))
                        {
                            if (p.HasDefaultValue)
                            {
                                return p.DefaultValue;
                            }
                            else if (isNullable || parameterType == AllowParameterType.String)
                            {
                                return null;
                            }

                            throw new InvalidOperationException($"Required parameter is not exist, Type: {p.ParameterType.FullName} ParameterName: {p.Name}");
                        }

                        return ParseAllowParameterType(parameterType, elementType, p, v);
                    }
                })
                .ToArray();

            return result;
        }

        static WorkloadInfo CreateWorkloadInfo(Type type, string name, IServiceProviderIsService isService, out Lazy<ObjectFactory> activator)
        {
            var ctors = type.GetConstructors();

            if (ctors.Length == 0)
            {
                activator = new Lazy<ObjectFactory>(() => ActivatorUtilities.CreateFactory(type, Array.Empty<Type>()));
                return new WorkloadInfo(name, Array.Empty<WorkloadParameterInfo>());
            }
            if (ctors.Length != 1)
            {
                throw new InvalidOperationException("Workload constructor only allows zero or one. Type:" + type.FullName);
            }

            var ctor = ctors[0];
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0)
            {
                activator = new Lazy<ObjectFactory>(() => ActivatorUtilities.CreateFactory(type, Array.Empty<Type>()));
                return new WorkloadInfo(name, Array.Empty<WorkloadParameterInfo>());
            }

            var parameterTypes = parameters.Where(x => !isService.IsService(x.ParameterType));
            var arguments = parameterTypes
                .Select(p =>
                {
                    var name = p.Name;
                    var enumNames = Array.Empty<string>();
                    if (p.ParameterType.IsEnum)
                    {
                        enumNames = Enum.GetNames(p.ParameterType);
                    }

                    var elementType = p.ParameterType;
                    var parameterType = ConvertToAllowParameterType(ref elementType, out var isNullable, out var isArray);
                    if (parameterType == null)
                    {
                        throw new InvalidOperationException($"Not allowed parameter type. Type:{ctor.DeclaringType!.FullName} Parameter:{type.FullName}");
                    }

                    var enumTypeName = (parameterType == AllowParameterType.Enum) ? elementType.Name : null;
                    var defaultValue = p.HasDefaultValue ? p.DefaultValue : null;
                    if (parameterType == AllowParameterType.Enum && p.HasDefaultValue)
                    {
                        defaultValue = Enum.GetName(elementType, defaultValue!);
                    }

                    return new WorkloadParameterInfo(parameterType.Value, isNullable, isArray, defaultValue, name!, enumNames, enumTypeName);
                })
                .ToArray();

            activator = new Lazy<ObjectFactory>(() => ActivatorUtilities.CreateFactory(type, parameterTypes.Select(x => x.ParameterType).ToArray()));

            return new WorkloadInfo(name, arguments);
        }

        static AllowParameterType? ConvertToAllowParameterType(ref Type type, out bool isNullable, out bool isArray)
        {
            if (type.IsArray)
            {
                isArray = true;
                type = type.GetElementType()!;
            }
            else
            {
                isArray = false;
            }

            var nt = Nullable.GetUnderlyingType(type);
            if (nt != null)
            {
                isNullable = true;
                type = nt;
            }
            else
            {
                isNullable = false;
            }

            // others...
            if (type.IsEnum) return AllowParameterType.Enum;
            if (type == typeof(Guid)) return AllowParameterType.Guid;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return AllowParameterType.Boolean;
                case TypeCode.Char:
                    return AllowParameterType.Char;
                case TypeCode.SByte:
                    return AllowParameterType.SByte;
                case TypeCode.Byte:
                    return AllowParameterType.Byte;
                case TypeCode.Int16:
                    return AllowParameterType.Int16;
                case TypeCode.UInt16:
                    return AllowParameterType.UInt16;
                case TypeCode.Int32:
                    return AllowParameterType.Int32;
                case TypeCode.UInt32:
                    return AllowParameterType.UInt32;
                case TypeCode.Int64:
                    return AllowParameterType.Int64;
                case TypeCode.UInt64:
                    return AllowParameterType.UInt64;
                case TypeCode.Single:
                    return AllowParameterType.Single;
                case TypeCode.Double:
                    return AllowParameterType.Double;
                case TypeCode.Decimal:
                    return AllowParameterType.Decimal;
                case TypeCode.DateTime:
                    return AllowParameterType.DateTime;
                case TypeCode.String:
                    return AllowParameterType.String;
                default:
                    return null;
            }
        }

        static object ParseAllowParameterType(AllowParameterType allowParameterType, Type elementType, ParameterInfo parameterInfo, string value)
        {
            switch (allowParameterType)
            {
                case AllowParameterType.Enum:
                    return Enum.Parse(elementType, value);
                case AllowParameterType.Boolean:
                    return bool.Parse(value);
                case AllowParameterType.Char:
                    return char.Parse(value);
                case AllowParameterType.SByte:
                    return sbyte.Parse(value);
                case AllowParameterType.Byte:
                    return byte.Parse(value);
                case AllowParameterType.Int16:
                    return short.Parse(value);
                case AllowParameterType.UInt16:
                    return ushort.Parse(value);
                case AllowParameterType.Guid:
                    return Guid.Parse(value);
                case AllowParameterType.Int32:
                    return int.Parse(value);
                case AllowParameterType.UInt32:
                    return uint.Parse(value);
                case AllowParameterType.Int64:
                    return long.Parse(value);
                case AllowParameterType.UInt64:
                    return ulong.Parse(value);
                case AllowParameterType.Single:
                    return float.Parse(value);
                case AllowParameterType.Double:
                    return double.Parse(value);
                case AllowParameterType.Decimal:
                    return decimal.Parse(value);
                case AllowParameterType.DateTime:
                    return DateTime.Parse(value);
                case AllowParameterType.String:
                    return value;
                default:
                    throw new InvalidOperationException($"Target parameter value can not parse, Type: {parameterInfo.ParameterType.FullName} Value: {value}");
            }
        }
    }
}