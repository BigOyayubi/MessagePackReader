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

            var reader = MessagePackReader2.MsgPackView.Create(bin);

            var keyBytes = MessagePackReader2.MsgPackView.KeyToSpan("Key");
            var intValueBytes = MessagePackReader2.MsgPackView.KeyToSpan("IntValue");
            var floatValueBytes = MessagePackReader2.MsgPackView.KeyToSpan("FloatValue");

            JetBrains.Profiler.Api.MeasureProfiler.StartCollectingData();
            foreach (var arrayValue in reader.AsArrayEnumerable())
            {
                arrayValue[keyBytes].GetString();
                arrayValue[intValueBytes].GetInt();
                arrayValue[floatValueBytes].GetFloat();
            }
            JetBrains.Profiler.Api.MeasureProfiler.SaveData();
        }
    }
}