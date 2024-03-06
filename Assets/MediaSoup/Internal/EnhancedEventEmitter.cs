using System;
using System.Threading.Tasks;

namespace Mediasoup.Internal
{
    public interface IEnhancedEventEmitter
    {
        Task<bool> SafeEmit(string name, params object[]? args);
        Task<bool> SafeEmit(string name, Action callback, Action<string> errorCallback, params object[]? args);
    }

    public interface IEnhancedEventEmitter<TEvent> : IEnhancedEventEmitter { }

    public class EnhancedEventEmitter : EventEmitter, IEnhancedEventEmitter
    {
        protected EnhancedEventEmitter()
        {
            
        }

        public async Task<bool> SafeEmit(string name, params object?[]? args)
        {
            var numListeners = ListenerCount(name);
            try
            {
                await Emit(name, args);
                return true;
            }
            catch (Exception e)
            {
                return numListeners > 0;
            }
        }

        public async Task<bool> SafeEmit(string name, Action callback, Action<string> errorCallback, params object[]? args)
        {
            var numListeners = ListenerCount(name);
            try
            {
                await Emit(name, args);
                callback?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                errorCallback?.Invoke(e.Message);
                return numListeners > 0;
            }
        }

        public async Task<T> SafeEmit<T>(string name, Func<Task<T>> callback, Action<string> errorCallback, params object[]? args)
        {
            var numListeners = ListenerCount(name);
            try
            {
                await Emit(name, args);
                return await callback.Invoke();
            }
            catch (Exception e)
            {
                errorCallback?.Invoke(e.Message);
                throw new Exception("SafeEmit");
            }
        }
    }

    public class EnhancedEventEmitter<TEvent> : EnhancedEventEmitter, IEnhancedEventEmitter<TEvent>
    {
        public EnhancedEventEmitter() : base()
        {

        }
    }

}