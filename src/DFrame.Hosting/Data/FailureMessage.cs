using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Hosting.Data
{
    public struct FailureMessage
    {
        public DateTimeOffset TimeStamp { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }

        public override bool Equals(object obj)
        {
            return obj is FailureMessage failure &&
                   TimeStamp.Equals(failure.TimeStamp) &&
                   Method == failure.Method &&
                   Path == failure.Path &&
                   Message == failure.Message;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TimeStamp, Method, Path, Message);
        }

        public static bool operator ==(FailureMessage left, FailureMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FailureMessage left, FailureMessage right)
        {
            return !(left == right);
        }
    }
}
