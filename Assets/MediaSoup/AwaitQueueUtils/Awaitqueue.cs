using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    private readonly Dictionary<int, PendingTask> pendingTasks = new Dictionary<int, PendingTask>();
    private int nextTaskId = 0;
    private bool stopping = false;

    public int Size => pendingTasks.Count;

    public async Task Push(AwaitQueueTask<object> task, string name = null)
    {
        name = name ?? task.Method.Name;

        Console.WriteLine($"Push() [name:{name}]");

        _ = await Task.Run<object>(() =>
        {
            var pendingTask = new PendingTask
            {
                Id = nextTaskId++,
                Task = task,
                Name = name,
                EnqueuedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ExecutedAt = null,
                Completed = false,
            };

            pendingTask.Reject = (error) => 
            {
                if (pendingTask.Completed)
                {
                    return;
                }

                pendingTask.Completed = true;

                pendingTasks.Remove(pendingTask.Id);

                if (!stopping)
                {
                    var nextPendingTask = pendingTasks.Values.FirstOrDefault();

                    if (nextPendingTask != null && !nextPendingTask.ExecutedAt.HasValue)
                    {
                        Execute(nextPendingTask);
                    }
                }
            };

            pendingTask.Resolve = (result) =>
            {
                if (pendingTask.Completed)
                {
                    return;
                }

                pendingTask.Completed = true;

                pendingTasks.Remove(pendingTask.Id);

                var nextPendingTask = pendingTasks.Values.FirstOrDefault();

                if (nextPendingTask != null && !nextPendingTask.ExecutedAt.HasValue)
                {
                    Execute(nextPendingTask);
                }
            };

            pendingTasks.Add(pendingTask.Id, pendingTask);

            if (pendingTasks.Count == 1)
            {
                Execute(pendingTask);
            }

            return default(object);

        });
    }

    public void Stop()
    {
        Console.WriteLine("Stop()");

        stopping = true;

        foreach (var pendingTask in pendingTasks.Values)
        {
            Console.WriteLine($"Stopping task [name:{pendingTask.Name}]");

            pendingTask.Reject(new AwaitQueueStoppedError());
        }

        stopping = false;
    }

    public void Remove(int taskIdx)
    {
        Console.WriteLine($"Remove() [taskIdx:{taskIdx}]");

        var pendingTask = pendingTasks.Values.ElementAtOrDefault(taskIdx);

        if (pendingTask != null)
        {
            pendingTask.Reject(new AwaitQueueRemovedTaskError());
        }
        else
        {
            Console.WriteLine($"No task with the given index [taskIdx:{taskIdx}]");
        }
    }

    public List<AwaitQueueTaskDump> Dump()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var idx = 0;

        return pendingTasks.Values.Select(pendingTask => new AwaitQueueTaskDump
        {
            Idx = idx++,
            Task = pendingTask.Task,
            Name = pendingTask.Name,
            EnqueuedTime = pendingTask.ExecutedAt.HasValue
                ? pendingTask.ExecutedAt.Value - pendingTask.EnqueuedAt
                : now - pendingTask.EnqueuedAt,
            ExecutionTime = pendingTask.ExecutedAt.HasValue
                ? now - pendingTask.ExecutedAt.Value
                : 0
        }).ToList();
    }

    private async void Execute(PendingTask pendingTask)
    {
        Console.WriteLine($"Execute() [name:{pendingTask.Name}]");

        if (pendingTask.ExecutedAt.HasValue)
        {
            throw new InvalidOperationException("Task already being executed");
        }

        pendingTask.ExecutedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            var result = pendingTask.Task.Invoke();

            pendingTask.Resolve(result);
        }
        catch (Exception error)
        {
            pendingTask.Reject(error);
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
