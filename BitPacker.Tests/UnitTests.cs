using System;
using NUnit.Framework;

namespace BitPacker.Tests
{
    [TestFixture]
    public class UnitTests
    {
        [ThreadStatic]
        private static Random ts_random;

        private static Random ThreadRandom => ts_random ?? (ts_random = new Random());

        [Test]
        public void RoundTripAllTypes()
        {
            var objA = Helpers.RandomFill<AllTypesExample>();
            var bytes = BitPack<AllTypesExample>.ToBytes(objA);
            var objB = BitPack<AllTypesExample>.FromBytes(bytes);

            Helpers.BitPackPropertiesAreEqual(objA, objB, true);
        }

        [Test]
        public void RoundTripCompactBools()
        {
            var objA = Helpers.RandomFill<CompactBoolsExample>();
            var bytes = BitPack<CompactBoolsExample>.ToBytes(objA);
            var objB = BitPack<CompactBoolsExample>.FromBytes(bytes);

            Helpers.BitPackPropertiesAreEqual(objA, objB, true);
        }

        [Test]
        public void RoundTripAllTypesWithOffsets()
        {
            var totalBytes = BitPack<AllTypesExample>.TotalBytes;
            var offset = ThreadRandom.Next(totalBytes, totalBytes * 2);
            var bytes = new byte[offset + totalBytes];

            var objA = Helpers.RandomFill<AllTypesExample>();
            BitPack<AllTypesExample>.WriteToByteArray(objA, bytes, offset);

            // ensure that nothing was written before the offset
            for (var i = 0; i < offset; i++)
            {
                if (bytes[i] != 0)
                    throw new Exception("Bytes were written before the offset.");
            }

            var objB = BitPack<AllTypesExample>.FromBytes(bytes, offset);

            Helpers.BitPackPropertiesAreEqual(objA, objB, true);
        }

        [Test]
        public void RoundTripCompactBoolsWithOffsets()
        {
            var totalBytes = BitPack<AllTypesExample>.TotalBytes;
            var offset = ThreadRandom.Next(totalBytes, totalBytes * 2);
            var bytes = new byte[offset + totalBytes];

            var objA = Helpers.RandomFill<CompactBoolsExample>();
            BitPack<CompactBoolsExample>.WriteToByteArray(objA, bytes, offset);

            // ensure that nothing was written before the offset
            for (var i = 0; i < offset; i++)
            {
                if (bytes[i] != 0)
                    throw new Exception("Bytes were written before the offset.");
            }

            var objB = BitPack<CompactBoolsExample>.FromBytes(bytes, offset);

            Helpers.BitPackPropertiesAreEqual(objA, objB, true);
        }
    }
}
