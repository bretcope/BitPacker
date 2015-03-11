using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitPacker.Tests
{
    public static class Helpers
    {
        public static T RandomFill<T>()
        {
            var properties = GetBitPackProperties<T>();
            var obj = Activator.CreateInstance<T>();
            var rand = new Random();

            foreach (var pi in properties)
            {
                object enumRand;
                Type type;
                object val;
                bool isEnum;
                if (pi.PropertyType.IsEnum)
                {
                    isEnum = true;
                    type = Enum.GetUnderlyingType(pi.PropertyType);
                    var enumNames = Enum.GetNames(pi.PropertyType);
                    enumRand = Enum.Parse(pi.PropertyType, enumNames[rand.Next(enumNames.Length)]);
                }
                else
                {
                    isEnum = false;
                    type = pi.PropertyType;
                    enumRand = null;
                }
                
                if (type == typeof(byte))
                {
                    val = isEnum ? (byte)enumRand : (byte)rand.Next(byte.MinValue, byte.MaxValue);
                }
                else if (type == typeof(sbyte))
                {
                    val = isEnum ? (sbyte)enumRand : (sbyte)rand.Next(sbyte.MinValue, sbyte.MaxValue);
                }
                else if (type == typeof(bool))
                {
                    val = rand.Next(0, 1) == 1;
                }
                else if (type == typeof(char))
                {
                    val = (char)rand.Next(char.MinValue, char.MaxValue);
                }
                else if (type == typeof(short))
                {
                    val = isEnum ? (short)enumRand : (short)rand.Next(short.MinValue, short.MaxValue);
                }
                else if (type == typeof(ushort))
                {
                    val = isEnum ? (ushort)enumRand : (ushort)rand.Next(ushort.MinValue, ushort.MaxValue);
                }
                else if (type == typeof(int))
                {
                    val = isEnum ? (int)enumRand : (int)rand.Next(int.MinValue, int.MaxValue);
                }
                else if (type == typeof(uint))
                {
                    val = isEnum ? (uint)enumRand : (uint)rand.Next(0, int.MaxValue);
                }
                else if (type == typeof(long))
                {
                    if (isEnum)
                    {
                        val = (long)enumRand;
                    }
                    else
                    {
                        val = (long)rand.Next(int.MinValue, int.MaxValue);
                        val = (long)val * (long)val;
                    }
                }
                else if (type == typeof(ulong))
                {
                    if (isEnum)
                    {
                        val = (ulong)enumRand;
                    }
                    else
                    {
                        val = (ulong) rand.Next(0, int.MaxValue);
                        val = (ulong) val*(ulong) val;
                    }
                }
                else if (type == typeof(float))
                {
                    val = (float)rand.NextDouble();
                }
                else if (type == typeof(double))
                {
                    val = rand.NextDouble();
                }
                else
                {
                    throw new NotImplementedException();
                }

                pi.SetValue(obj, val);
            }

            return obj;
        }

        public static bool BitPackPropertiesAreEqual<T>(T a, T b, bool assert = false)
        {
            var properties = GetBitPackProperties<T>();
            foreach (var pi in properties)
            {
                if (!pi.GetValue(a).Equals(pi.GetValue(b)))
                {
                    if (assert)
                        throw new Exception("Property " + pi.Name + " was not equal. " + pi.GetValue(a) + " != " + pi.GetValue(b));

                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<PropertyInfo> GetBitPackProperties<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.GetCustomAttribute<BitPackAttribute>() != null);
        }
    }
}