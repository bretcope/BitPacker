namespace BitPacker.Tests
{
    public enum TestEnum
    {
        Zero,
        One,
        Two,
        Three,
        Four,
        Five
    }

    public class AllTypesExample
    {
        [BitPack] public bool Bool_A { get; set; }
        [BitPack] public bool Bool_B { get; set; }
        [BitPack] public char Char_A { get; set; }
        [BitPack] public char Char_B { get; set; }
        [BitPack] public sbyte Int8_A { get; set; }
        [BitPack] public sbyte Int8_B { get; set; }
        [BitPack] public byte UInt8_A { get; set; }
        [BitPack] public byte UInt8_B { get; set; }
        [BitPack] public short Int16_A { get; set; }
        [BitPack] public short Int16_B { get; set; }
        [BitPack] public ushort UInt16_A { get; set; }
        [BitPack] public ushort UInt16_B { get; set; }
        [BitPack] public int Int32_A { get; set; }
        [BitPack] public int Int32_B { get; set; }
        [BitPack] public uint UInt32_A { get; set; }
        [BitPack] public uint UInt32_B { get; set; }
        [BitPack] public long Int64_A { get; set; }
        [BitPack] public long Int64_B { get; set; }
        [BitPack] public ulong UInt64_A { get; set; }
        [BitPack] public ulong UInt64_B { get; set; }
        [BitPack] public TestEnum Enum_A { get; set; }
        [BitPack] public TestEnum Enum_B { get; set; }
    }

    [BitPackOptions(BitPackMode.CompactBools)]
    public class CompactBoolsExample
    {
        [BitPack] public bool Bool_A { get; set; }
        [BitPack] public bool Bool_B { get; set; }
        [BitPack] public char Char_A { get; set; }
        [BitPack] public char Char_B { get; set; }
        [BitPack] public sbyte Int8_A { get; set; }
        [BitPack] public sbyte Int8_B { get; set; }
        [BitPack] public byte UInt8_A { get; set; }
        [BitPack] public byte UInt8_B { get; set; }
        [BitPack] public short Int16_A { get; set; }
        [BitPack] public short Int16_B { get; set; }
        [BitPack] public ushort UInt16_A { get; set; }
        [BitPack] public ushort UInt16_B { get; set; }
        [BitPack] public int Int32_A { get; set; }
        [BitPack] public int Int32_B { get; set; }
        [BitPack] public uint UInt32_A { get; set; }
        [BitPack] public uint UInt32_B { get; set; }
        [BitPack] public long Int64_A { get; set; }
        [BitPack] public long Int64_B { get; set; }
        [BitPack] public ulong UInt64_A { get; set; }
        [BitPack] public ulong UInt64_B { get; set; }
        [BitPack] public TestEnum Enum_A { get; set; }
        [BitPack] public TestEnum Enum_B { get; set; }
    }
}