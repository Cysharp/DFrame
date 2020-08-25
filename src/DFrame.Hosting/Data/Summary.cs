using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Hosting.Data
{
    public class Summary
    {
        public string ExecuteId { get; set; }
        public string Host { get; set; }
        public string Status { get; set; }
        public int Workers { get; set; }
        public double Rps { get; set; }
        public double Failures { get; set; }
    }
}
