using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public struct LogMessage
    {
        public DateTimeOffset TimeStamp { get; set; }
        public string Message { get; set; }

        public override bool Equals(object obj)
        {
            return obj is LogMessage message &&
                   TimeStamp.Equals(message.TimeStamp) &&
                   Message == message.Message;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TimeStamp, Message);
        }

        public static bool operator ==(LogMessage left, LogMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LogMessage left, LogMessage right)
        {
            return !(left == right);
        }
    }
}
