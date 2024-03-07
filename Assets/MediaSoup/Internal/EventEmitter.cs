using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Mediasoup.Internal 
{
    public delegate Task EventHandler(params object?[]? args);

    public interface IEventEmitter 
    {
        IDisposable On(string name, Func<object[]?, Task> handler);
        IDisposable AddEventListener(string name, Func<object[]?, Task> handler);

        void Off(string name, Func<object[]?, Task> handler);
        void RemoveListener(string name, Func<object[]?, Task> handler);

        void RemoveAllListeners(string name);
        Task Emit(string name, params object[]? data);
    }

    public class EventEmitter
    {
        private class EventListener : IDisposable
        {
            public EventListener(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            private Action? disposeAction;

            public void Dispose()
            {
                if (disposeAction == null) return;
                disposeAction.Invoke();
                disposeAction = null;
            }
        }

        private readonly Dictionary<string, EventHandler> namedHandlers = new();
        

        private EventHandler CreateHandlers(string name)
        {
            if (namedHandlers.TryGetValue(name, out var handlers)) return handlers;
            if (namedHandlers.TryGetValue(name, out handlers)) return handlers;
            handlers = null;
            namedHandlers.Add(name, handlers);
            return handlers;
        }

        public void On(string name, EventHandler handler)
        {
            EventHandler tuple = CreateHandlers(name);

            if (tuple == null) 
            {
                namedHandlers[name] += handler;
            }

            tuple += handler;
        }

        /*public IDisposable On<TArgs>(string name,TArgs genOb, EventHandler handler)
        {
            var tuple = CreateHandlers(name);
            tuple.Item1 += handler;
            return new EventListener(() => tuple.Item1 -= handler);
        }*/

        public void Once(string name, EventHandler handler)
        {
            EventHandler tuple = CreateHandlers(name);
            EventHandler? h = null;
            h = async args =>
            {
                await handler(args);
                tuple -= h;
            };
            tuple += h;
        }

        public void AddEventListener(string name, EventHandler handler) => On(name, handler);

        public void Off(string name, EventHandler handler)
        {
            if (!namedHandlers.TryGetValue(name, out var tuple)) return;
            tuple -= handler;
        }

        public void RemoveListener(string name, EventHandler handler) => Off(name, handler);

        public void RemoveAllListeners(string name)
        {
            if (!namedHandlers.TryGetValue(name, out _)) return;
            namedHandlers.Remove(name);
        }

        public async Task Emit(string name, params object?[]? data)
        {
            if (!namedHandlers.TryGetValue(name, out var handlers)) 
            {
                return;
            }

            Debug.Log($"{name} Does exist");
            if (handlers == null) Debug.Log($"handler is null");
            await handlers.Invoke(data);
        }



        protected int ListenerCount(string name) => namedHandlers.TryGetValue(name, out var list)
            ? list!.GetInvocationList().Length
            : 0;

    }
}


