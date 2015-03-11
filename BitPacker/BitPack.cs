using System;
using System.Collections.Generic;
using System.Reflection;
using Sigil;

// ReSharper disable StaticMemberInGenericType
namespace BitPacker
{
    public static class BitPack<T>
    {
        private static readonly Func<T, byte[], int, byte[]> s_toBytes;
        private static readonly Func<byte[], int, T> s_fromBytes;

        private static readonly bool s_packBools;

        public static int TotalBytes { get; }
        public static BitPackMode Mode { get; }

        private static readonly Dictionary<Type, AcceptedType> s_acceptedTypes = new Dictionary<Type, AcceptedType>
        {
            [typeof(bool)] = new AcceptedType { Size = sizeof(bool), FromBytes = typeof(BitConverter).GetMethod("ToBoolean") },
            [typeof(char)] = new AcceptedType { Size = sizeof(char), FromBytes = typeof(BitConverter).GetMethod("ToChar") },
            [typeof(sbyte)] = new AcceptedType { Size = sizeof(sbyte), FromBytes = null },
            [typeof(byte)] = new AcceptedType { Size = sizeof(byte), FromBytes = null },
            [typeof(short)] = new AcceptedType { Size = sizeof(short), FromBytes = typeof(BitConverter).GetMethod("ToInt16") },
            [typeof(ushort)] = new AcceptedType { Size = sizeof(ushort), FromBytes = typeof(BitConverter).GetMethod("ToUInt16") },
            [typeof(int)] = new AcceptedType { Size = sizeof(int), FromBytes = typeof(BitConverter).GetMethod("ToInt32") },
            [typeof(uint)] = new AcceptedType { Size = sizeof(uint), FromBytes = typeof(BitConverter).GetMethod("ToUInt32") },
            [typeof(long)] = new AcceptedType { Size = sizeof(long), FromBytes = typeof(BitConverter).GetMethod("ToInt64") },
            [typeof(ulong)] = new AcceptedType { Size = sizeof(ulong), FromBytes = typeof(BitConverter).GetMethod("ToUInt64") },
            [typeof(float)] = new AcceptedType { Size = sizeof(float), FromBytes = typeof(BitConverter).GetMethod("ToSingle") },
            [typeof(double)] = new AcceptedType { Size = sizeof(double), FromBytes = typeof(BitConverter).GetMethod("ToDouble") },
        };

        private class AcceptedType
        {
            public int Size { get; set; }
            public MethodInfo FromBytes { get; set; }
        }

        static BitPack()
        {
            var type = typeof(T);

            var options = type.GetCustomAttribute<BitPackOptionsAttribute>();
            Mode = options?.Mode ?? BitPackMode.NoCompaction;
            s_packBools = Mode != BitPackMode.NoCompaction;

            // get all bit pack properties
            var allProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Array.Sort(allProperties, (a, b) => String.CompareOrdinal(a.Name, b.Name));

            var infos = new List<PackInfo>();
            var bools = new List<PropertyInfo>();

            var offset = 0;
            foreach (var pi in allProperties)
            {
                var attr = pi.GetCustomAttribute<BitPackAttribute>();
                if (attr == null)
                    continue;

                if (pi.PropertyType == typeof(bool))
                {
                    bools.Add(pi);
                    continue;
                }

                int size;
                Type backing;
                if (pi.PropertyType.IsEnum)
                {
                    backing = Enum.GetUnderlyingType(pi.PropertyType);
                    size = s_acceptedTypes[backing].Size;
                }
                else
                {
                    if (!s_acceptedTypes.ContainsKey(pi.PropertyType))
                    {
                        throw new Exception(
                            String.Format("Cannot bit pack {0}.{1} of type {2}. It is not a supported type.", type.FullName, pi.Name, pi.PropertyType.FullName));
                    }

                    size = s_acceptedTypes[pi.PropertyType].Size;
                    backing = pi.PropertyType;
                }

                infos.Add(new PackInfo { ByteOffset = offset, PropertyInfo = pi, Size = size, BackingType = backing });
                offset += size;
            }

            // setup bools
            var bit = 0;
            foreach (var pi in bools)
            {
                infos.Add(new PackInfo { ByteOffset = offset, PropertyInfo = pi, Size = 1, Bit = bit, BackingType = typeof(bool) });

                if (!s_packBools)
                    bit = 8;
                else
                    bit++;

                if (bit == 8)
                {
                    offset++;
                    bit = 0;
                }
            }

            if (bit > 0)
                offset++;

            TotalBytes = offset;
            s_toBytes = GetToBytesFunc(infos);
            s_fromBytes = GetFromBytesFunc(infos);
        } 

        public static byte[] ToBytes(T obj)
        {
            var bytes = new byte[TotalBytes];
            return s_toBytes(obj, bytes, 0);
        }

        public static void WriteToByteArray(T obj, byte[] bytes, int offset)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            if (offset + TotalBytes > bytes.Length)
            {
                throw new InvalidOperationException(
                    String.Format("Cannot write {0} to byte array. Needed {1} bytes, and there are only {2} remaining bytes after offset.", typeof(T).Name, TotalBytes, bytes.Length - offset));
            }

            s_toBytes(obj, bytes, offset);
        }

        public static T FromBytes(byte[] bytes, int offset = 0)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            if (offset + TotalBytes > bytes.Length)
            {
                throw new InvalidOperationException(
                    String.Format("Cannot get {0} from bytes. Expected {1} and got {2} remaining bytes after offset.", typeof(T).Name, TotalBytes, bytes.Length - offset));
            }

            return s_fromBytes(bytes, offset);
        }

        private static Func<T, byte[], int, byte[]> GetToBytesFunc(List<PackInfo> infos)
        {
            var type = typeof (T);
            var blockCopy = typeof (Buffer).GetMethod("BlockCopy");

            var emit = Emit<Func<T, byte[], int, byte[]>>.NewDynamicMethod(type.Name + "_ToBytes");

            const int objArg = 0;    // T obj
            const int bytesArg = 1;  // bytes[] bytes
            const int offsetArg = 2; // int offset

            // write data for each property
            foreach (var info in infos)
            {
                if (info.IsBool && s_packBools)
                {
                    // the property is a bool, and we're bit-packing bools

                    // get property value
                    emit.LoadArgument(objArg);                     // [objArg]
                    emit.CallVirtual(info.PropertyInfo.GetMethod); // [value]

                    // we only want to do anything if the value is true (no reason to OR a zero)
                    var endIf = emit.DefineLabel();
                    emit.BranchIfFalse(endIf); // empty

                    // load the array index address
                    emit.LoadArgument(bytesArg);        // [bytesArg]
                    emit.LoadConstant(info.ByteOffset); // [bytesArg] [info-offset]
                    emit.LoadArgument(offsetArg);       // [bytesArg] [info-offset] [offsetArg]
                    emit.Add();                         // [bytesArg] [offset]
                    emit.LoadElementAddress<byte>();    // [ref bytesArg[offset]]

                    // OR the existing value of the byte with the bit flag
                    var flag = (byte)(1 << info.Bit);
                    emit.Duplicate();        // [ref bytesArg[offset]] [ref bytesArg[offset]]
                    emit.LoadObject<byte>(); // [ref bytesArg[offset]] [bytesArg[offset]]
                    emit.LoadConstant(flag); // [ref bytesArg[offset]] [bytesArg[offset]] [flag]
                    emit.Or();               // [ref bytesArg[offset]] [OR'd-int]
                    emit.Convert<byte>();    // [ref bytesArg[offset]] [OR'd-byte]

                    // write the OR'd byte back to the array
                    emit.StoreObject<byte>(); // empty

                    // end of if statement
                    emit.MarkLabel(endIf);
                }
                else if (info.BackingType == typeof(byte) || info.BackingType == typeof(sbyte))
                {
                    // byte types can be written directly with no conversion

                    // load the array index address
                    emit.LoadArgument(bytesArg);        // [bytesArg]
                    emit.LoadConstant(info.ByteOffset); // [bytesArg] [offset]
                    emit.LoadArgument(offsetArg);       // [bytesArg] [info-offset] [offsetArg]
                    emit.Add();                         // [bytesArg] [offset]
                    emit.LoadElementAddress<byte>();    // [ref bytesArg[offset]]

                    // get property value
                    emit.LoadArgument(objArg);                     // [ref bytesArg[offset]] [objArg]
                    emit.CallVirtual(info.PropertyInfo.GetMethod); // [ref bytesArg[offset]] [value]

                    // store the property value into the array address
                    emit.StoreObject<byte>(); // empty
                }
                else
                {
                    // this is a type we need to convert to bytes

                    // get converter method
                    var getBytes = typeof(BitConverter).GetMethod("GetBytes", new[] { info.BackingType });

                    // get property value
                    emit.LoadArgument(objArg);                     // [objArg]
                    emit.CallVirtual(info.PropertyInfo.GetMethod); // [value]

                    // convert the value to bytes
                    emit.Call(getBytes); // [bytes]

                    // copy the bytes to the overall array
                    // Buffer.BlockCopy(Array src, int srcOffset, Array dst, int dstOffset, int count)
                    emit.LoadConstant(0);               // [bytes] [0]
                    emit.LoadArgument(bytesArg);        // [bytes] [0] [bytesArg]
                    emit.LoadConstant(info.ByteOffset); // [bytes] [0] [bytesArg] [info-offset]
                    emit.LoadArgument(offsetArg);       // [bytes] [0] [bytesArg] [info-offset] [offsetArg]
                    emit.Add();                         // [bytes] [0] [bytesArg] [offset]
                    emit.LoadConstant(info.Size);       // [bytes] [0] [bytesArg] [offset] [size]
                    emit.Call(blockCopy);               // empty
                }
            }

            emit.LoadArgument(bytesArg); // [bytesArg]
            emit.Return();

            return emit.CreateDelegate();
        }

        private static Func<byte[], int, T> GetFromBytesFunc(List<PackInfo> infos)
        {
            var emit = Emit<Func<byte[], int, T>>.NewDynamicMethod(typeof(T).Name + "_ToBytes");

            const int bytesArg = 0;  // bytes[] bytes
            const int offsetArg = 1; // int offset

            // create instance of object
            var obj = emit.DeclareLocal<T>("obj");
            emit.NewObject<T>();  // [new T]
            emit.StoreLocal(obj); // empty

            // read each property from the byte array
            foreach (var info in infos)
            {
                if (info.IsBool && s_packBools)
                {
                    // property is a bit flag

                    // load the object
                    emit.LoadLocal(obj); // [obj]

                    // load the appropriate byte
                    emit.LoadArgument(bytesArg);        // [obj] [bytesArg]
                    emit.LoadConstant(info.ByteOffset); // [obj] [bytesArg] [info-offset]
                    emit.LoadArgument(offsetArg);       // [obj] [bytesArg] [info-offset] [offsetArg]
                    emit.Add();                         // [obj] [bytesArg] [offset]
                    emit.LoadElement<byte>();           // [obj] [byte]

                    // check if the flag is set
                    var flag = 1 << info.Bit;
                    emit.LoadConstant(flag); // [obj] [byte] [flag]
                    emit.And();              // [obj] [byte & flag]
                    emit.LoadConstant(flag); // [obj] [byte & flag] [flag]
                    emit.CompareEqual();     // [obj] [isEqual]

                    // set the property
                    emit.CallVirtual(info.PropertyInfo.SetMethod); // empty
                }
                else if (info.BackingType == typeof(byte) || info.BackingType == typeof(sbyte))
                {
                    // just read the byte directly

                    // load the object
                    emit.LoadLocal(obj); // [obj]

                    // load the byte
                    emit.LoadArgument(bytesArg);        // [obj] [bytesArg]
                    emit.LoadConstant(info.ByteOffset); // [obj] [bytesArg] [info-offset]
                    emit.LoadArgument(offsetArg);       // [obj] [bytesArg] [info-offset] [offsetArg]
                    emit.Add();                         // [obj] [bytesArg] [offset]
                    emit.LoadElement<byte>();           // [obj] [byte]

                    // set the property
                    emit.CallVirtual(info.PropertyInfo.SetMethod); // empty
                }
                else
                {
                    // need to convert from a byte array

                    // load the object
                    emit.LoadLocal(obj); // [obj]

                    // load the arguments for the converter
                    emit.LoadArgument(bytesArg);        // [obj] [bytesArg]
                    emit.LoadConstant(info.ByteOffset); // [obj] [bytesArg] [info-offset]
                    emit.LoadArgument(offsetArg);       // [obj] [bytesArg] [info-offset] [offsetArg]
                    emit.Add();                         // [obj] [bytesArg] [offset]

                    // convert to the value
                    emit.Call(s_acceptedTypes[info.BackingType].FromBytes); // [obj] [value]

                    // set the property
                    emit.CallVirtual(info.PropertyInfo.SetMethod); // empty
                }
            }

            emit.LoadLocal(obj); // [obj]
            emit.Return();

            return emit.CreateDelegate();
        }
    }

    internal class PackInfo
    {
        private static readonly Type s_boolType = typeof(bool);

        public bool IsBool => BackingType == s_boolType;
        public int Bit { get; set; }
        public int Size { get; set; }
        public int ByteOffset { get; set; }
        public Type BackingType { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
    }

    public enum BitPackMode
    {
        NoCompaction = 0,
        CompactBools = 1
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class BitPackOptionsAttribute : Attribute
    {
        public BitPackMode Mode { get; }

        public BitPackOptionsAttribute(BitPackMode mode)
        {
            Mode = mode;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class BitPackAttribute : Attribute
    {
    }
}
