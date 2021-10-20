using System;
using System.Reflection;
using UnityEngine;

namespace UShell
{
    public static class Extensions
    {
        public static bool EndsWith(this string str, char[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (str.EndsWith(values[i].ToString()))
                    return true;
            }

            return false;
        }
        public static string ReplaceFirst(this string str, string oldValue, string newValue)
        {
            int oldValuePos = str.IndexOf(oldValue, 0, StringComparison.Ordinal);
            if (oldValuePos >= 0)
                return str.Substring(0, oldValuePos) + newValue + str.Substring(oldValuePos + oldValue.Length, str.Length - (oldValuePos + oldValue.Length));

            return str;
        }

        public static bool IsAwaitable(this MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            return IsAwaitable(method.ReturnType);
        }
        public static bool IsAwaitable(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            MethodInfo getAwaiter = type.GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (getAwaiter != null)
                return IsAwaiter(getAwaiter.ReturnType);

            return false;
        }
        public static bool IsAwaiter(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.GetInterface("System.Runtime.CompilerServices.ICriticalNotifyCompletion") != null;
        }
    }
}
