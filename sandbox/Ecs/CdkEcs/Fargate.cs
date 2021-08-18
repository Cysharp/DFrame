using System;

namespace Cdk
{
    /// <summary>
    /// FargateSpec follow to the rule.
    /// https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task-cpu-memory-error.html
    /// </summary>
    public class Fargate
    {
        /// <summary>
        /// Cpu Size to use
        /// </summary>
        public int CpuSize => _cpuSize;
        private int _cpuSize;
        /// <summary>
        /// Memory Size to use
        /// </summary>
        public int MemorySize => _memorysize;
        private int _memorysize;

        public Fargate() : this(CpuSpec.Quater, MemorySpec.Low)
        {
        }
        public Fargate(CpuSpec cpu, MemorySpec memory)
        {
            _cpuSize = (int)cpu;
            _memorysize = CalculateMemorySize(cpu, memory);
        }
        public Fargate(CpuSpec cpu, int memorySize)
        {
            _cpuSize = (int)cpu;
            _memorysize = CalculateMemorySize(cpu, memorySize);
        }

        /// <summary>
        /// Memory Calculation for Typical MemorySize
        /// </summary>
        /// <param name="cpu"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private int CalculateMemorySize(CpuSpec cpu, MemorySpec memory) => (int)cpu * (int)memory;
        /// <summary>
        /// Memory Calculation for Custom MemorySize
        /// </summary>
        /// <param name="memorySize"></param>
        /// <returns></returns>
        private int CalculateMemorySize(CpuSpec cpu, int memorySize)
        {
            switch (cpu)
            {
                case CpuSpec.Quater:
                case CpuSpec.Half:
                case CpuSpec.Single:
                    throw new ArgumentOutOfRangeException($"You must select CpuSpec of Double or Quadruple.");
                case CpuSpec.Double:
                    {
                        // 4096 < n < 16384, n can be increments of 1024
                        if (memorySize % 1024 != 0)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} must be increments of 1024.");
                        if (memorySize < _cpuSize * 2)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too low, must be larger than {_cpuSize * 2}");
                        if (memorySize > _cpuSize * 4)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too large, must be lower than {_cpuSize * 4}");
                    }
                    break;
                case CpuSpec.Quadruple:
                    {
                        // 8192 < n < 30720, n can be increments of 1024
                        if (memorySize % 1024 != 0)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} must be increments of 1024.");
                        if (memorySize < _cpuSize * 2)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too low, must be larger than {_cpuSize * 2}");
                        if (memorySize > _cpuSize * 7.5)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too large, must be lower than {_cpuSize * 7.5}");
                    }
                    break;
            }
            return memorySize;
        }

        /// <summary>
        /// Fargate Cpu Spec. default 256 = 0.25
        /// </summary>
        public enum CpuSpec
        {
            Quater = 256,
            Half = 512,
            Single = 1024,
            Double = 2048,
            Quadruple = 4096,
        }

        /// <summary>
        /// Fargate Memory Spec. default 512 = 0.5GB
        /// </summary>
        public enum MemorySpec
        {
            /// <summary>
            /// Only available when <see cref="CpuSpec"/> is Double or Quadruple.
            /// </summary>
            Custom = 0,
            Low = 2,
            Medium = 4,
            High = 8,
        }
    }
}
