using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DFrame
{
    internal class DFrameWorkerCollection
    {
        readonly Dictionary<string, DFrameWorkerTypeInfo> dframeTypes;

        public static DFrameWorkerCollection FromCurrentAssemblies()
        {
            return FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        }

        public static DFrameWorkerCollection FromAssemblies(Assembly[] searchAssemblies)
        {
            return new DFrameWorkerCollection(searchAssemblies);
        }

        DFrameWorkerCollection(Assembly[] searchAssemblies)
        {
            dframeTypes = new Dictionary<string, DFrameWorkerTypeInfo>(StringComparer.InvariantCultureIgnoreCase);

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

                foreach (var worker in types)
                {
                    if (typeof(Worker).IsAssignableFrom(worker) && !worker.IsAbstract)
                    {
                        var attr = worker.GetCustomAttribute<WorkerAttribute>(false);
                        var master = GetMasterType(worker);
                        var name = attr?.Name ?? worker.Name;
                        var disallowSingleExecute = attr?.DisallowSingleExecute ?? false;

                        var t = new DFrameWorkerTypeInfo(worker, master, name, disallowSingleExecute);
                        if (!dframeTypes.TryAdd(name, t))
                        {
                            throw new InvalidOperationException($"Worker name is duplicate. name:{name}, type:{t.WorkerType.FullName}");
                        }
                    }
                }
            }
        }

        static Type? GetMasterType(Type t)
        {
            while (t.BaseType != null)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Worker<>))
                {
                    return t.GetGenericArguments()[0];
                }
                t = t.BaseType;
            }
            return null;
        }

        public IEnumerable<DFrameWorkerTypeInfo> All => dframeTypes.Values;

        public bool TryGetWorker(string workerName, [NotNullWhen(true)] out DFrameWorkerTypeInfo? worker)
        {
            return dframeTypes.TryGetValue(workerName, out worker);
        }
    }



    internal class DFrameWorkerTypeInfo
    {
        public Type WorkerType { get; }
        public Type? MasterType { get; }
        public string Name { get; }
        public bool DisallowSingleExecute { get; }

        public DFrameWorkerTypeInfo(Type workerType, Type? masterType, string name, bool disallowSingleExecute)
        {
            WorkerType = workerType;
            MasterType = masterType;
            Name = name;
            DisallowSingleExecute = disallowSingleExecute;
        }
    }

}