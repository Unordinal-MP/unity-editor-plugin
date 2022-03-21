using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Unordinal.Editor.Utils
{
    //http://blog.i3arnon.com/2017/02/21/task-wrapper/
    public class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly IDisposable _releaser;
        private readonly Task<IDisposable> _releaserTask;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _releaser = new Releaser(_semaphore);
            _releaserTask = Task.FromResult(_releaser);
        }

        public TaskWrapper<IDisposable> LockAsync()
        {
            var waitTask = _semaphore.WaitAsync();
            return new TaskWrapper<IDisposable>(
                waitTask.IsCompleted
                    ? _releaserTask
                    : waitTask.ContinueWith(
                        (_, releaser) => (IDisposable)releaser,
                        _releaser,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default));
        }

        public struct TaskWrapper<T>
        {
            private readonly Task<T> _task;

            public TaskWrapper(Task<T> task)
            {
                _task = task;
            }

            public TaskAwaiter<T> GetAwaiter() => _task.GetAwaiter();
        }

        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
