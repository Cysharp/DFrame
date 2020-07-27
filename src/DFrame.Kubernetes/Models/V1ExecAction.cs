using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1ExecAction
    {
        public IList<string> command { get; set; }
    }
}
