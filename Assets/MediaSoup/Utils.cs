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

    }
}