using System;
using NUnit.Framework;

namespace BitPacker.Tests
{
    [TestFixture]
    public class UnitTests
    {
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
    }
}
