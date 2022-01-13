using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DFrame.Tests
{
    public class DistributedCollectionTest
    {
        ITestOutputHelper helper;

        public DistributedCollectionTest(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public async Task Queue()
        {
            var r = await TestHelper.RunDFrameAsync<QueueBatch, bool>(helper);
            r.Should().BeTrue();
        }

        [Fact]
        public async Task Dict()
        {
            var r = await TestHelper.RunDFrameAsync<DictionaryBatch, bool>(helper);
            r.Should().BeTrue();
        }
    }

    class QueueBatch : Workload
    {
        ResultBox<bool> box;

        public QueueBatch(ResultBox<bool> box)
        {
            this.box = box;
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var q = context.CreateDistributedQueue<int>("QueueBatchTest");

            q.Key.Should().Be("QueueBatchTest");

            await q.EnqueueAsync(0);
            await q.EnqueueAsync(1);

            var v1 = await q.TryDequeueAsync();
            v1.HasValue.Should().BeTrue();
            v1.Value.Should().Be(0);

            var v2 = await q.TryDequeueAsync();
            v2.HasValue.Should().BeTrue();
            v2.Value.Should().Be(1);

            var v3 = await q.TryDequeueAsync();
            v3.HasValue.Should().BeFalse();

            await q.EnqueueAsync(2);
            await q.EnqueueAsync(3);
            await q.EnqueueAsync(4);

            var c = await q.GetCountAsync();
            c.Should().Be(3);

            var p = await q.TryPeekAsync();
            p.HasValue.Should().BeTrue();
            p.Value.Should().Be(2);

            var xs = await q.ToArrayAsync();
            xs.Should().BeEquivalentTo(new[] { 2, 3, 4 });

            box.Value = true;
        }
    }

    class DictionaryBatch : Workload
    {
        ResultBox<bool> box;

        public DictionaryBatch(ResultBox<bool> box)
        {
            this.box = box;
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var dict = context.CreateDistributedDictionary<string, int>("DictionaryBatchTest");

            dict.Key.Should().Be("DictionaryBatchTest");

            await dict.AddAsync("foo", 100);

            var v = await dict.TryGetValueAsync("foo");
            v.HasValue.Should().BeTrue();
            v.Value.Should().Be(100);

            box.Value = true;
        }
    }
}
