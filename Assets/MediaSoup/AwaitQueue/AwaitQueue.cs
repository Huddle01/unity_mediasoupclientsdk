using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;

namespace Huddle01.Utils 
{
    public class AwaitQueue
    {
        private Dictionary<int, object> _pendingTasks = new Dictionary<int, object>();

        private int _taskNumber = 0;
        private int _currentExecutingTask = 0;

        public AwaitQueue()
        {

        }

        public async Task Push<T>(Func<object[], Task<T>> body, Action<T> callback, params object[] args)
        {
            if (body == null) return;
            _taskNumber++;

            PendingTaskClass<T> taskToExecute = new PendingTaskClass<T>
            {
                Execution = body,
                Callback = callback,
                Arguments = args,
                TaskNumber = _taskNumber
            };
            Debug.Log($"Adding task {_taskNumber}");
            _pendingTasks.Add(_taskNumber, taskToExecute as object);

            if (_pendingTasks.Count == 1)
            {
                Execute(taskToExecute);
            }
        }

        private async void Execute<T>(PendingTaskClass<T> pendingTask)
        {
            Debug.Log($"Executing task {pendingTask.TaskNumber}");

            _currentExecutingTask = pendingTask.TaskNumber;
            if (!pendingTask.ShouldRemove)
            {
                var result = await pendingTask.Execution(pendingTask.Arguments);

                pendingTask?.Callback?.Invoke(result);
            }

            _pendingTasks.Remove(pendingTask.TaskNumber);

            _currentExecutingTask++;
            object nextTask;

            if (_pendingTasks.TryGetValue(_currentExecutingTask, out nextTask))
            {
                var genericArgumentType = nextTask.GetType().GetGenericArguments()[0];

                if (genericArgumentType == typeof(int))
                {
                    PendingTaskClass<int> nextTaskToExe = nextTask as PendingTaskClass<int>;
                    Execute<int>(nextTaskToExe);
                }
                else if (genericArgumentType == typeof(bool))
                {
                    PendingTaskClass<bool> nextTaskToExe = nextTask as PendingTaskClass<bool>;
                    Execute<bool>(nextTaskToExe);
                }
                else if (genericArgumentType == typeof(float))
                {
                    PendingTaskClass<float> nextTaskToExe = nextTask as PendingTaskClass<float>;
                    Execute<float>(nextTaskToExe);
                }

            }
            else
            {

                return;
            }
        }
    }

    public class PendingTaskClass<T>
    {
        public Func<object[], Task<T>> Execution;
        public Action<T> Callback;
        public object[] Arguments;
        public int TaskNumber;
        public Action<Exception> ErrorCallback;
        public bool ShouldRemove = false;
    }
}


