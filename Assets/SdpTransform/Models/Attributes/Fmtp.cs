﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Utilme.SdpTransform
{
    public class Fmtp
    {
        public const string Label = "fmtp:";
        public byte PayloadType { get; set; }
        public string Value { get; set; }
    }
}
