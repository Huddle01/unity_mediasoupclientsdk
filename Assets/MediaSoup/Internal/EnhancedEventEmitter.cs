using System;
using System.Threading.Tasks;

namespace Mediasoup.Internal
{
    public interface IEnhancedEventEmitter
    {
        Task<bool> SafeEmit(string name, params object[]? args);
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
    }

    public class EnhancedEventEmitter<TEvent> : EnhancedEventEmitter, IEnhancedEventEmitter<TEvent>
    {
        public EnhancedEventEmitter() : base()
        {

        }
    }

}