using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFrame
{
    internal static class Shims
    {
        internal static bool TryAdd(this Dictionary<string, DFrameWorkloadTypeInfo> dict, string key, DFrameWorkloadTypeInfo value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
                return true;
            }
            return false;
        }
    }

    internal delegate object ObjectFactory(IServiceProvider serviceProvider, object[] arguments);

    internal static class ActivatorUtilities
    {
        internal static ObjectFactory CreateFactory(Type type, Type[] _)
        {
            return (__, args) => Activator.CreateInstance(type, args);
        }
    }
}