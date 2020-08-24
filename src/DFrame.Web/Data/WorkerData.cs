using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public struct WorkerData
    {
        /// <summary>
        /// Name of worker machine
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// State of worker. running, ready, stopped, launching....
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// State is running.
        /// </summary>
        public bool IsRunning => State == "Running";
        /// <summary>
        /// Worker users
        /// </summary>
        public int Users { get; set; }
        /// <summary>
        /// Percentage of worker cpu
        /// </summary>
        public double Cpu { get; set; }

        public override bool Equals(object obj)
        {
            return obj is WorkerData data &&
                   Name == data.Name &&
                   State == data.State &&
                   IsRunning == data.IsRunning &&
                   Users == data.Users &&
                   Cpu == data.Cpu;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, State, IsRunning, Users, Cpu);
        }

        public static bool operator ==(WorkerData left, WorkerData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WorkerData left, WorkerData right)
        {
            return !(left == right);
        }
    }
}
