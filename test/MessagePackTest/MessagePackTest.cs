using System;
using NUnit.Framework;
using MiniMessagePack;

namespace MessagePackTest
{

    [TestFixture]
    public class MessagePackTest
    {
        public static void Main() { }

        [Test]
        public void TestByte()
        {
            for(byte i = byte.MinValue; i <= byte.MaxValue; i++)
            {
                var bin = Serialize(i);
                var reader = MiniMessagePack.MsgPack.Deserialize(bin);
                Assert.AreEqual(reader.GetByte(), i);
            }
        }

        [Test]
        public void TestSByte()
        {
            for(sbyte i = sbyte.MinValue; i <= sbyte.MaxValue; i++)
            {
                var bin = Serialize(i);
                var reader = MiniMessagePack.MsgPack.Deserialize(bin);
                Assert.AreEqual(reader.GetSByte(), i);
            }
        }

        [Test]
        public void TestShort()
        {
            for(short i = short.MinValue; i <= short.MaxValue; i++)
            {
                var bin = Serialize(i);
                var reader = MiniMessagePack.MsgPack.Deserialize(bin);
                Assert.AreEqual(reader.GetShort(), i);
            }
        }

        byte[] Serialize<T>(T value)
        {
            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<T>();
            byte[] bin;
            using (var stream = new System.IO.MemoryStream())
            {
                serializer.Pack(stream, value);
                bin = stream.ToArray();
            }
            return bin;
        }
    }
}
