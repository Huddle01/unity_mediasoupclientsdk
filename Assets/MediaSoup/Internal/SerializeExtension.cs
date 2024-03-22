using System;
using System.Runtime.Serialization;

public static class SerializeExtension
{
    
    public static T New<T>(this Type type) => (T)FormatterServices.GetUninitializedObject(type);
}
