using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Models
{
    public struct Failure
    {
        public int Fails { get; set; }
        public string Method { get; set; }
        public string Name { get; set; }
        public Exception Type { get; set; }
    }
}
