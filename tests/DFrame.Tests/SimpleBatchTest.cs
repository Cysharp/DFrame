#pragma warning disable CS1998

using FluentAssertions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DFrame.Tests
{
    public class SimpleBatchTest
    {
        ITestOutputHelper helper;

        public SimpleBatchTest(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public async Task ExecuteTest()
        {
            var v = await TestHelper.RunDFrameAsync<FirstWorker, int>(helper);
            v.Should().Be(100);
        }
    }

    public class FirstWorker : Worker
    {
        ResultBox<int> box;

        public FirstWorker(ResultBox<int> box)
        {
            this.box = box;
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            box.Value = 100;
        }
    }
}
