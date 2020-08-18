using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public struct Worker
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
    }
}
