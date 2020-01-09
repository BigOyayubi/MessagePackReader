using System.Linq;
using System.Collections.Generic;

namespace MessagePackProfiling
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            IList<SampleModel1> arrayObjects =
                Enumerable.Range(0, 1000).Select(_ => new SampleModel1().Set(_)).ToList();
            var bin = MessagePack.MessagePackSerializer.Serialize(arrayObjects);

            JetBrains.Profiler.Api.MeasureProfiler.StartCollectingData();
            Profile(bin);
            Profile2(bin);
            JetBrains.Profiler.Api.MeasureProfiler.SaveData();
        }

        private static void Profile(byte[] bytes)
        {
            var reader = MessagePackReader.MsgPackView.Create(bytes);

            var keyBytes = MessagePackReader.MsgPackView.KeyToBytes("Key");
            var intValueBytes = MessagePackReader.MsgPackView.KeyToBytes("IntValue");
            var floatValueBytes = MessagePackReader.MsgPackView.KeyToBytes("FloatValue");

            foreach (var arrayValue in reader.AsArrayEnumerable())
            {
                arrayValue[keyBytes].GetString();
                arrayValue[intValueBytes].GetInt();
                arrayValue[floatValueBytes].GetFloat();
            }
        }

        private static void Profile2(byte[] bytes)
        {
            var reader = MessagePackReader2.MsgPackView.Create(bytes);

            var keyBytes = MessagePackReader2.MsgPackView.KeyToSpan("Key");
            var intValueBytes = MessagePackReader2.MsgPackView.KeyToSpan("IntValue");
            var floatValueBytes = MessagePackReader2.MsgPackView.KeyToSpan("FloatValue");

            foreach (var arrayValue in reader.AsArrayEnumerable())
            {
                arrayValue[keyBytes].GetString();
                arrayValue[intValueBytes].GetInt();
                arrayValue[floatValueBytes].GetFloat();
            }
        }
    }
}