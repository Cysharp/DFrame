using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public class Summary
    {
        public string Host { get; set; }
        public string Status { get; set; }
        public int Worker { get; set; }
        public double Rps { get; set; }
        public int Failures { get; set; }
    }
}
