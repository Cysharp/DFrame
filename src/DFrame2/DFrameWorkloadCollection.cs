using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DFrame
{
    internal class DFrameWorkloadCollection
    {
        readonly Dictionary<string, DFrameWorkloadTypeInfo> dframeTypes;

        public static DFrameWorkloadCollection FromCurrentAssemblies()
        {
            return FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        }

        public static DFrameWorkloadCollection FromAssemblies(Assembly[] searchAssemblies)
        {
            return new DFrameWorkloadCollection(searchAssemblies);
        }

        DFrameWorkloadCollection(Assembly[] searchAssemblies)
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

                        var t = new DFrameWorkloadTypeInfo(workload, name);
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

        public DFrameWorkloadTypeInfo(Type workloadType, string name)
        {
            WorkloadType = workloadType;
            Name = name;
        }
    }
}