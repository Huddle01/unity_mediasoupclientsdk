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
            else
            {
                // Assuming T is a reference type or a value type that can be serialized/deserialized.
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
            }
        }

    }
}