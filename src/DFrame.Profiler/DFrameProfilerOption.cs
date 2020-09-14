using DFrame.Profiler.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.Profiler
{
    public class DFrameProfilerOption
    {
        public bool EnableProfiler { get; set; }
        public string ProductVersion { get; set; } = Assembly.GetEntryAssembly().GetName().Version.ToString();
        public Action<DbContextOptionsBuilder> OnConfiguring { get; set; }
        public Action<ModelBuilder> OnModelCreating { get; set; }
        public Func<ProfileHistory, CancellationToken, Task> OnPreInsertAsync { get; set; }
        public Func<ProfileHistory, CancellationToken, Task> OnPostInsertAsync { get; set; }
    }
}
