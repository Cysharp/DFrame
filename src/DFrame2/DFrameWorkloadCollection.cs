using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DFrame
{
    internal class DFrameWorkloadCollection
    {
        readonly Dictionary<string, DFrameWorkloadTypeInfo> dframeTypes;

        public static DFrameWorkloadCollection FromCurrentAssemblies(IServiceProviderIsService isService)
        {
            return FromAssemblies(AppDomain.CurrentDomain.GetAssemblies(), isService);
        }

        public static DFrameWorkloadCollection FromAssemblies(Assembly[] searchAssemblies, IServiceProviderIsService isService)
        {
            return new DFrameWorkloadCollection(searchAssemblies, isService);
        }

        DFrameWorkloadCollection(Assembly[] searchAssemblies, IServiceProviderIsService isService)
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
        public Type WorkloadType { get; }
        public string Name { get; }
        public WorkloadInfo WorkloadInfo { get; }

        public DFrameWorkloadTypeInfo(Type workloadType, string name, IServiceProviderIsService isService)
        {
            WorkloadType = workloadType;
            Name = name;
            WorkloadInfo = CreateWorkloadInfo(workloadType, name, isService);
        }

        static WorkloadInfo CreateWorkloadInfo(Type type, string name, IServiceProviderIsService isService)
        {
            var ctors = type.GetConstructors();

            if (ctors.Length == 0)
            {
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
                return new WorkloadInfo(name, Array.Empty<WorkloadParameterInfo>());
            }

            var arguments = parameters
                .Where(x => !isService.IsService(x.ParameterType))
                .Select(p =>
                {
                    var name = p.Name;
                    var enumNames = Array.Empty<string>();
                    if (p.ParameterType.IsEnum)
                    {
                        enumNames = Enum.GetNames(p.ParameterType);
                    }

                    var parameterType = ConvertToAllowParameterType(p.ParameterType, out var isNullable, out var isArray);
                    if (parameterType == null)
                    {
                        throw new InvalidOperationException($"Not allowed parameter type. Type:{ctor.DeclaringType!.FullName} Parameter:{type.FullName}");
                    }

                    return new WorkloadParameterInfo(parameterType.Value, isNullable, isArray, p.DefaultValue, name!, enumNames);
                })
                .ToArray();

            return new WorkloadInfo(name, arguments);
        }

        static AllowParameterType? ConvertToAllowParameterType(Type type, out bool isNullable, out bool isArray)
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
                    // others...
                    if (type.IsEnum) return AllowParameterType.Enum;
                    return null;
            }
        }
    }
}