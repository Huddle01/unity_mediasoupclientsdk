using System;

namespace Utilme.SdpTransform 
{
    public class CustomAttributes
    {
    
    }

    public class StringValueAttribute : Attribute
    {
        public string Value { get; }

        public StringValueAttribute(string value)
        {
            Value = value;
        }
    }
}


