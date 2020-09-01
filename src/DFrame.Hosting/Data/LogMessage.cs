using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Hosting.Data
{
    public struct LogMessage
    {
        public DateTimeOffset TimeStamp { get; set; }
        public string Message { get; set; }
    }
}
