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
        /// env sample: `DFRAME_WORKER_NODESELECTOR='KEY1=FOO;KEY2=BAR'` will become `KEY1 = FOO` and `KEY2 = BAR`entries.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IDictionary<string, string> GetNodeSelectors(string key)
        {
            // pick up `DFRAME_WORKER_NODESELECTOR` entries
            var nodeSelectors = _data
                .Where(x => string.Equals(x.Key,key, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.Value.Split(';')) // ['KEY1=FOO', 'KEY2=BAR']
                .Select(x =>
                {
                    var entry = x.Split('=');
                    if (entry.Length != 2)
                        return null;
                    return entry; // [KEY1, FOO], [KEY2, BAR]
                })
                .Where(x => x != null)
                .ToDictionary(kv => kv[0], kv => kv[1]);

            return nodeSelectors;
        }
    }
}