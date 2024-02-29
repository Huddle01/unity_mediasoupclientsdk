using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Mediasoup 
{
    public static class Utils
    {
        public static T Clone<T>(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
            {
                return default;
            }
            else if (double.IsNaN(Convert.ToDouble(value)))
            {
                // Assuming T is a numeric type (e.g., double) for the NaN check.
                return (T)(object)double.NaN;
            }
            else
            {
                // Assuming T is a reference type or a value type that can be serialized/deserialized.
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
            }
        }
    }
}