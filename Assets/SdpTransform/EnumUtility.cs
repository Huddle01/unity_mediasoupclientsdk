using System;
using System.Reflection;

namespace Utilme.SdpTransform 
{
    public static class EnumUtility
    {
        public static TEnum EnumFromStringValue<TEnum>(this string stringValue) where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }

            foreach (var field in typeof(TEnum).GetFields())
            {
                if (field.GetCustomAttribute(typeof(StringValueAttribute)) is StringValueAttribute attribute)
                {
                    if (attribute.Value == stringValue)
                    {
                        return (TEnum)field.GetValue(null);
                    }
                }
            }

            throw new ArgumentException($"No enum value with display name '{stringValue}' found for type {typeof(TEnum)}");
        }

        public static string GetStringValue<TEnum>(this TEnum? enumValue) where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }

            var field = typeof(TEnum).GetField(enumValue.ToString());
            if (field?.GetCustomAttribute(typeof(StringValueAttribute)) is StringValueAttribute attribute)
            {
                return attribute.Value;
            }

            return enumValue.ToString();
        }

        public static string GetStringValue<TEnum>(this TEnum enumValue) where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }

            var field = typeof(TEnum).GetField(enumValue.ToString());
            if (field?.GetCustomAttribute(typeof(StringValueAttribute)) is StringValueAttribute attribute)
            {
                return attribute.Value;
            }

            return enumValue.ToString();
        }
    }
}


