using System.Collections;

namespace DFrame.Kubernetes.Internals
{
    internal class EnvironmentVariablesProvider
    {
        public IDictionary<string, string> Data { get; private set; }

        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public EnvironmentVariablesProvider() : this(string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance with the specified prefix.
        /// </summary>
        /// <param name="prefix">A prefix used to filter the environment variables.</param>
        public EnvironmentVariablesProvider(string prefix)
        {
            _prefix = prefix ?? string.Empty;
            Load();
        }

        /// <summary>
        /// Load Current Environment Variables.
        /// </summary>
        public void Load()
        {
            Load(Environment.GetEnvironmentVariables());
        }

        private void Load(IDictionary envVariables)
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var filteredEnvVariables = envVariables
                .Cast<DictionaryEntry>()
                .Select(x =>
                {
                    var key = (string)x.Key;
                    x.Key = NormalizeKey(key);
                    return x;
                })
                .Where(entry => ((string)entry.Key).StartsWith(_prefix, StringComparison.OrdinalIgnoreCase));

            foreach (var envVariable in filteredEnvVariables)
            {
                var key = ((string)envVariable.Key).Substring(_prefix.Length);
                Data[key] = (string)envVariable.Value;
            }
        }

        private static string NormalizeKey(string key)
        {
            // replace FOO__BAR to FOO:BAR
            return key.Replace("__", ":");
        }
    }

    /// <summary>
    /// Offer EnvironmentVariables Source with specific Data type.
    /// </summary>
    /// Usage Samples.....
    /// // pattern 1. use prefix.
    /// var provider = new EnvironmentVariablesProvider("DFRAME_WORKER_");
    /// provider.Dump();
    /// var source1 = new EnvironmentVariablesSource("DFRAME_WORKER_");
    /// source1.GetNodeSelectors("NODESELECTOR").Dump("pattern 1.");
    /// 
    /// // pattern 2. use full env key.
    /// var source2 = new EnvironmentVariablesSource(string.Empty);
    /// source2.GetNodeSelectors("DFRAME_WORKER_NODESELECTOR").Dump("pattern 2.");
    internal class EnvironmentVariablesSource
    {
        private IDictionary<string, string> _data;

        public EnvironmentVariablesSource(string prefix)
        {
            var provider = new EnvironmentVariablesProvider(prefix);
            _data = provider.Data;
        }

        /// <summary>
        /// Get Kubernetse NodeSelector from Environment Variables.
        /// env sample: `DFRAME_WORKER_NODESELECTOR__0__KEY = VALUE` will become `KEY = VALUE` entries.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public IDictionary<string, string> GetNodeSelectors(string prefix)
        {
            // pick up `DFRAME_WORKER_NODESELECTOR*` entries
            var nodeSelectors = _data
                .Where(x => x.Key.StartsWith(prefix))
                .Select((x, i) =>
                {
                    // DFRAME_WORKER_NODESELECTOR:0:KEY -> KEY
                    var key = x.Key
                        .Substring(prefix.Length) // DFRAME_WORKER_NODESELECTOR:0:KEY -> :0:KEY
                        .Substring($":{i}:".Length); // :0:KEY -> KEY
                    return (key, x.Value);
                })
                .ToDictionary(kv => kv.key, kv => kv.Value);

            return nodeSelectors;
        }
    }
}