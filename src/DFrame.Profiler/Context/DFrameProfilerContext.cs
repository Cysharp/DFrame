using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace DFrame.Profiler.Context
{
    public class DFrameProfilerContext : DbContext
    {
        private readonly DFrameProfilerOption _profilerOption;
        public DFrameProfilerContext(DbContextOptions<DFrameProfilerContext> options)
            : base(options)
        {
            _profilerOption = this.GetService<DFrameProfilerOption>();
        }

        public DbSet<ProfileHistory> ProfileHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            _profilerOption?.OnConfiguring?.Invoke(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            _profilerOption?.OnModelCreating?.Invoke(modelBuilder);
        }
    }

    public class ProfileHistory
    {
        [Key]
        [Required]
        [Column(Order = 0)]
        public string HistoryId { get; set; } = DateTime.UtcNow.ToString("yyyyMMddHHMMss");
        [Required]
        [Column(Order = 1)]
        public string ContextId { get; set; }
        [Required]
        [Column(Order = 2)]
        public string ProductVersion { get; set; } = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();
        [Required]
        [Column(Order = 3)]
        public string WorkerName { get; set; }
        [Required]
        [Column(Order = 4)]
        public string Argument { get; set; }

        // Load
        [Column(Order = 5)]
        public int Requests { get; set; }
        [Column(Order = 6)]
        public int Errors { get; set; }
        [Column(Order = 7)]
        public double Duration { get; set; }
    }
}
