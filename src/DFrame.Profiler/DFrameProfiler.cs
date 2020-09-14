using DFrame.Profiler.Context;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.Profiler
{
    public interface IDFrameProfiler
    {
        ValueTask InsertAsync(string contextId, string workerName, string[] arguments, int requests, int errors, TimeSpan Duration, CancellationToken token);
        ValueTask InsertAsync(ProfileHistory entity, CancellationToken token);
    }

    public class DFrameProfiler : IDFrameProfiler
    {
        private readonly DFrameProfilerContext _context;
        private readonly DFrameProfilerOption _option;

        public DFrameProfiler(DFrameProfilerContext context, DFrameProfilerOption option)
        {
            _context = context;
            _option = option;
        }

        public async ValueTask InsertAsync(string contextId, string workerName, string[] arguments, int requests, int errors, TimeSpan duration, CancellationToken token = default)
        {
            // run only if profiler is enabled.
            if (_option.EnableProfiler)
            {
                var entity = new ProfileHistory
                {
                    ContextId = contextId,
                    WorkerName = workerName,
                    Argument = string.Join(" ", arguments),
                    Requests = requests,
                    Errors = errors,
                    Duration = duration.TotalSeconds,
                };

                if (_option?.OnPreInsertAsync != null)
                {
                    await _option?.OnPreInsertAsync.Invoke(entity, token);
                }

                await _context.AddAsync<ProfileHistory>(entity, token);
                await _context.SaveChangesAsync(token);

                if (_option?.OnPostInsertAsync != null)
                {
                    await _option?.OnPostInsertAsync.Invoke(entity, token);
                }
            }
        }

        public async ValueTask InsertAsync(ProfileHistory entity, CancellationToken token = default)
        {
            if (_option.EnableProfiler)
            {
                if (_option?.OnPreInsertAsync != null)
                {
                    await _option?.OnPreInsertAsync.Invoke(entity, token);
                }

                await _context.AddAsync<ProfileHistory>(entity, token);
                await _context.SaveChangesAsync(token);

                if (_option?.OnPostInsertAsync != null)
                {
                    await _option?.OnPostInsertAsync.Invoke(entity, token);
                }
            }
        }
    }
}
