using System.Text;

namespace System
{
    public static class StringExtensions
    {
        
        public static bool IsNullOrEmpty(this string? source)
        {
            return string.IsNullOrEmpty(source);
        }

        public static bool IsNullOrWhiteSpace(this string? source)
        {
            return string.IsNullOrWhiteSpace(source);
        }

        public static string NullOrWhiteSpaceReplace(this string? source, string newValue)
        {
            return !string.IsNullOrWhiteSpace(source) ? source : newValue;
        }

        public static string NullOrEmptyReplace(this string? source, string newValue)
        {
            return !string.IsNullOrEmpty(source) ? source : newValue;
        }
        
        public static string FormatWith(this string format, object arg0)
        {
            return string.Format(format, arg0);
        }

        public static string FormatWith(this string format, object arg0, object arg1)
        {
            return string.Format(format, arg0, arg1);
        }

        public static string FormatWith(this string format, object arg0, object arg1, object arg2)
        {
            return string.Format(format, arg0, arg1, arg2);
        }

        public static string FormatWith(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
        {
            return string.Format(provider, format, args);
        }

        
        public static string Repeat(this string source, int times)
        {
            if (string.IsNullOrEmpty(source) || times <= 0)
            {
                return source;
            }

            var sb = new StringBuilder();
            while (times > 0)
            {
                sb.Append(source);
                times--;
            }

            return sb.ToString();
        }
        
        public static string? ToNullableString<T>(this T source) where T : class
        {
            return source?.ToString();
        }

        public static string ToEmptyableString<T>(this T? source) where T : class
        {
            return source != null ? source.ToString()! : string.Empty;
        }
    }
}
