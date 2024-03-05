using Mediasoup.RtpParameter;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Unity.VisualScripting;

namespace System
{
    public static class EnumExtensions
    {
        #region DescriptionAttribute

        public static T GetValueByDescription<T>(this string description) where T : Enum
        {
            var type = typeof(T);
            foreach (var field in type.GetFields())
            {
                var curDesc = field.GetDescriptAttributes();
                if (curDesc?.Length > 0)
                {
                    if (curDesc[0].Description == description)
                    {
                        return (T)field.GetValue(null)!;
                    }
                }
                else
                {
                    if (field.Name == description)
                    {
                        return (T)field.GetValue(null)!;
                    }
                }
            }

            throw new ArgumentException("The corresponding enumeration could not be found.", nameof(description));
        }

        private static DescriptionAttribute[]? GetDescriptAttributes(this FieldInfo fieldInfo)
        {
            return fieldInfo != null ? (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) : null;
        }

        //public static string GetDescription(this Enum enumValue)
        //{
        //    var type = enumValue.GetType();
        //    var enumName = Enum.GetName(type, enumValue) ?? throw new NotSupportedException();

        //    var attribute = type.GetField(enumName)!.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
        //    if (attribute != null)
        //    {
        //        var displayName = ((DisplayAttribute)attribute).Name;
        //        if (displayName != null)
        //        {
        //            return displayName;
        //        }
        //    }

        //    return enumValue.ToString();
        //}

        //public static IEnumerable<KeyValuePair<T, string>> GetDescriptionMap<T>(this Type type) where T : Enum
        //{
        //    return from e in Enum.GetValues(type).Cast<T>()
        //           select new KeyValuePair<T, string>(e, e.GetDescription());
        //}

        //public static IEnumerable<KeyValuePair<T, string>> GetDescriptionMap<T>() where T : Enum
        //{
        //    return GetDescriptionMap<T>(typeof(T));
        //}

        #endregion DescriptionAttribute

        #region DisplayAttribute

        //public static string GetDisplayName(this Enum enumValue)
        //{
        //    var type = enumValue.GetType();
        //    var enumName = Enum.GetName(type, enumValue) ?? throw new NotSupportedException();

        //    var attribute = type.GetField(enumName)!.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault();
        //    if (attribute != null)
        //    {
        //        var displayName = ((DisplayAttribute)attribute).Name;
        //        if (displayName != null)
        //        {
        //            return displayName;
        //        }
        //    }

        //    return enumValue.ToString();
        //}

        //public static IEnumerable<KeyValuePair<T, string>> GetDisaplayNameMap<T>(this Type type) where T : Enum
        //{
        //    return from e in Enum.GetValues(type).Cast<T>()
        //           select new KeyValuePair<T, string>(e, e.GetDisplayName());
        //}

        //public static IEnumerable<KeyValuePair<T, string>> GetDisaplayNameMap<T>() where T : Enum
        //{
        //    return GetDisaplayNameMap<T>(typeof(T));
        //}

        #endregion DisplayAttribute

        #region EnumMemberAttribute

        public static string GetEnumMemberValue(this Enum enumValue)
        {
            var type = enumValue.GetType();
            var enumName = Enum.GetName(type, enumValue) ?? throw new NotSupportedException();

            var attribute = type.GetField(enumName)!.GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault();
            if (attribute != null)
            {
                var value = ((EnumMemberAttribute)attribute).Value;
                if (value != null)
                {
                    return value;
                }
            }

            return enumValue.ToString();
        }

        public static T GetEnumValueFromEnumMemberValue<T>(string enumMemberValue)
        {
            foreach (T enumValue in Enum.GetValues(typeof(T)))
            {
                MemberInfo memberInfo = typeof(RtpHeaderExtensionUri).GetField(enumValue.ToString());
                EnumMemberAttribute attribute = memberInfo.GetCustomAttribute<EnumMemberAttribute>();
                if (attribute != null && attribute.Value == enumMemberValue)
                {
                    return enumValue;
                }
            }

            throw new ArgumentException("The corresponding enumeration could not be found.", enumMemberValue);
        }

        public static IEnumerable<KeyValuePair<T, string>> GetEnumMemberValueMap<T>(this Type type) where T : Enum
        {
            return from e in Enum.GetValues(type).Cast<T>()
                   select new KeyValuePair<T, string>(e, e.GetEnumMemberValue());
        }

        //public static IEnumerable<KeyValuePair<T, string>> GetEnumMemberValueMap<T>() where T : Enum
        //{
        //    return GetDisaplayNameMap<T>(typeof(T));
        //}

        #endregion EnumMemberAttribute

        #region RawConstantValue

        public static int GetInt32(this Enum enumValue)
        {
            var type = enumValue.GetType();
            var enumName = Enum.GetName(type, enumValue)!;
            var enumFieldInfo = type.GetField(enumName);
            return (int)enumFieldInfo!.GetRawConstantValue()!;
        }

        #endregion RawConstantValue
    }
}

