using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace MessagePackTest
{

    [TestFixture]
    public class MessagePackTest
    {
        public static void Main() { }



        [Test]
        public void TestByte()
        {
            byte[] array = 
            {
                byte.MinValue,
                byte.MaxValue/2,
                byte.MaxValue,
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetByte());
        }

        [Test]
        public void TestSByte()
        {
            sbyte[] array =
            {
                sbyte.MinValue,
                -1,
                0,
                1,
                sbyte.MaxValue,
            };

            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetSByte());
        }

        [Test]
        public void TestShort()
        {
            short[] array =
            {
                short.MinValue,
                sbyte.MinValue,
                -1,
                0,
                1,
                sbyte.MaxValue,
                byte.MaxValue,
                short.MaxValue
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetShort());
        }

        [Test]
        public void TestUShort()
        {
            ushort[] array =
            {
                ushort.MinValue,
                (ushort)sbyte.MaxValue,
                byte.MaxValue,
                (ushort)short.MaxValue,
                ushort.MaxValue
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetUShort() );
        }

        [Test]
        public void TestInt()
        {
            int[] array =
            {
                int.MinValue,
                short.MinValue,
                sbyte.MinValue,
                -1,
                0,
                1,
                sbyte.MaxValue,
                byte.MaxValue,
                short.MaxValue,
                ushort.MaxValue,
                int.MaxValue,
            };

            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetInt());
        }

        [Test]
        public void TestUInt()
        {
            uint[] array =
            {
                uint.MinValue,
                1,
                (uint)sbyte.MaxValue,
                byte.MaxValue,
                (uint)short.MaxValue,
                ushort.MaxValue,
                (uint)int.MaxValue,
                uint.MaxValue
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetUInt());
        }

        [Test]
        public void TestLong()
        {
            long[] array =
            {
                long.MinValue,
                int.MinValue,
                short.MinValue,
                sbyte.MinValue,
                -1,
                0,
                1,
                sbyte.MaxValue,
                byte.MaxValue,
                short.MaxValue,
                ushort.MaxValue,
                int.MaxValue,
                long.MaxValue,
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetLong());
        }

        [Test]
        public void TestULong()
        {
            ulong[] array =
            {
                ulong.MinValue,
                1,
                (ulong)sbyte.MaxValue,
                byte.MaxValue,
                (uint)short.MaxValue,
                ushort.MaxValue,
                int.MaxValue,
                uint.MaxValue,
                ulong.MaxValue,
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetULong());
        }

        [Test]
        public void TestFloat()
        {
            float[] array =
            {
                float.MinValue,
                float.MinValue/2f,
                -1,
                0,
                1,
                float.MaxValue/2f,
                float.MaxValue
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetFloat());
        }

        [Test]
        public void TestDouble()
        {
            double[] array =
            {
                double.MinValue,
                double.MinValue/2f,
                float.MinValue,
                float.MinValue/2f,
                -1,
                0,
                1,
                float.MaxValue/2f,
                float.MaxValue,
                double.MaxValue/2f,
                double.MaxValue
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetDouble());
        }

        [Test]
        public void TestBoolean()
        {
            bool[] array =
            {
                false,
                true
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetBool());
        }

        [Test]
        public void TestString()
        {
            string[] array =
            {
                "",
                "aaaa",
                "ああああ",
            };
            TestArray(array, (ref MiniMessagePack.Reader reader) => reader.GetString());
        }

        [Test]
        public void TestBinary()
        {
            var baseString = "あいうえお";
            byte[] array = System.Text.Encoding.UTF8.GetBytes(baseString);
            var bin = Serialize(array);
            var reader = MiniMessagePack.Reader.Deserialize(bin);
            var unpacked = reader.GetBinary();
            Assert.AreEqual(array.SequenceEqual(unpacked), true);
            var s = System.Text.Encoding.UTF8.GetString(unpacked);
            Assert.AreEqual(baseString, s);
        }

        [Test]
        public void TestArray()
        {
            object[] array = { byte.MinValue, int.MinValue, "あいうえお" };
            var bin = Serialize(array);
            var reader = MiniMessagePack.Reader.Deserialize(bin);
            Assert.AreEqual(reader[0].GetByte(), array[0]);
            Assert.AreEqual(reader[1].GetInt(), array[1]);
            Assert.AreEqual(reader[2].GetString(), array[2]);
        }

        [Test]
        public void TestMap()
        {
            var map = new Dictionary<string, object>()
            {
                { "byte", byte.MinValue },
                { "int",  int.MinValue },
                { "string", "あいうえお" },
            };

            var bin = Serialize(map);
            var reader = MiniMessagePack.Reader.Deserialize(bin);
            Assert.AreEqual(reader["byte"].GetByte(), map["byte"] );
            Assert.AreEqual(reader["int"].GetInt(), map["int"]);
            Assert.AreEqual(reader["string"].GetString(), map["string"]);
        }

        [Test]
        public void TestComplex()
        {
            var map = new Dictionary<string, object>()
            {
                { "byte", byte.MinValue },
                { "int",  int.MinValue },
                { "string", "あいうえお" },
                { "array", new object[]{ byte.MinValue, int.MinValue, float.MinValue, "あいうえお" } },
            };
            var bin = Serialize(map);
            var reader = MiniMessagePack.Reader.Deserialize(bin);
            Assert.AreEqual(reader["byte"].GetByte(), map["byte"]);
            Assert.AreEqual(reader["int"].GetInt(), map["int"]);
            Assert.AreEqual(reader["array"][0].GetByte(),  ((object[])map["array"])[0]);
            Assert.AreEqual(reader["array"][1].GetInt(),   ((object[])map["array"])[1]);
            Assert.AreEqual(reader["array"][2].GetFloat(), ((object[])map["array"])[2]);
            Assert.AreEqual(reader["array"][3].GetString(), ((object[])map["array"])[3]);
        }

        delegate T ArrayTravarse<T>(ref MiniMessagePack.Reader reader);
        void TestArray<T>(T[] array, ArrayTravarse<T> travarse)
        {
            foreach (var i in array)
            {
                Console.WriteLine("test " + i);
                var bin = Serialize(i);

                var reader = MiniMessagePack.Reader.Deserialize(bin);
                Assert.AreEqual( travarse.Invoke(ref reader), i);
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
                stream.Position = 0;
                var unpack = serializer.Unpack(stream);
            }
            return bin;
        }
    }
}
