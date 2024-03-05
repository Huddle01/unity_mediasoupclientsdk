using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mediasoup 
{
    public class Utils
    {

        private static readonly System.Random _random = new();

        public static uint GenerateRandomNumber()
        {
            return (uint)_random.Next(100_000_000, 1_000_000_000);
        }

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