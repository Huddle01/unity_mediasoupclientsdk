using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Huddle01.Backup 
{
    public delegate Task<object> AwaitQueueTask<T>();

    public class AwaitQueueTaskDump
    {
        public int Idx { get; set; }
        public AwaitQueueTask<object> Task { get; set; }
        public string Name { get; set; }
        public long EnqueuedTime { get; set; }
        public long ExecutionTime { get; set; }
    }

    public class AwaitQueueStoppedError : Exception
    {
        public AwaitQueueStoppedError(string message = "AwaitQueue stopped") : base(message)
        {
        }
    }

    public class AwaitQueueRemovedTaskError : Exception
    {
        public AwaitQueueRemovedTaskError(string message = "AwaitQueue task removed") : base(message)
        {
        }
    }

    public class AwaitQueue
    {
        private readonly Queue<Func<Task>> _tasks = new Queue<Func<Task>>();
        private bool _isExecuting = false;

        public Task<T> Push<T>(Func<Task<T>> taskGenerator, string name)
        {
            var tcs = new TaskCompletionSource<T>();

            EnqueueInternal(async () =>
            {
                try
                {
                    var result = await taskGenerator();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    // If the task fails, forward the exception to the TaskCompletionSource
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        private void EnqueueInternal(Func<Task> taskGenerator)
        {
            bool shouldStart = false;

            lock (_tasks)
            {
                _tasks.Enqueue(taskGenerator);
                if (!_isExecuting)
                {
                    _isExecuting = true;
                    shouldStart = true;
                }
            }

            if (shouldStart)
            {
                Task.Run(async () => await StartExecuting());
            }
        }

        private async Task StartExecuting()
        {
            while (true)
            {
                Func<Task> taskGenerator;

                lock (_tasks)
                {
                    if (_tasks.Count == 0)
                    {
                        _isExecuting = false;
                        return;
                    }

                    taskGenerator = _tasks.Dequeue();
                }

                // Execute the task outside of the lock to allow for other tasks to be enqueued.
                await taskGenerator();
            }
        }
    }

    public class PendingTask
    {
        public int Id { get; set; }
        public AwaitQueueTask<object> Task { get; set; }
        public string Name { get; set; }
        public long EnqueuedAt { get; set; }
        public long? ExecutedAt { get; set; }
        public bool Completed { get; set; }
        public Action<object> Resolve { get; set; }
        public Action<Exception> Reject { get; set; }
    }

}

