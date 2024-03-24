using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace System
{
    public static class ObjectExtensions
    {
        public static bool IsNumericType(this object o)
        {
            return Type.GetTypeCode(o.GetType()) switch
            {
                TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.UInt16
                or TypeCode.UInt32
                or TypeCode.UInt64
                or TypeCode.Int16
                or TypeCode.Int32
                or TypeCode.Int64
                or TypeCode.Decimal
                or TypeCode.Double
                or TypeCode.Single => true,
                _ => false,
            };
        }

        public static bool IsStringType(this object o)
        {
            return Type.GetTypeCode(o.GetType()) switch
            {
                TypeCode.String => true,
                _ => false
            }; ;
        
        }
    }
}
