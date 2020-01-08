using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackProfiling
{
    [MessagePack.MessagePackObject]
    public class SampleModel2
    {
        public SampleModel2 Set(int i)
        {
            Key = i.ToString();
            IntValue = i;
            FloatValue = (float)i;
            DoubleValue = (double)i;
            return this;
        }

        [MessagePack.Key(0)]
        public string Key { get; set; }
        [MessagePack.Key(1)]
        public int IntValue { get; set; }
        [MessagePack.Key(2)]
        public float FloatValue { get; set; }
        [MessagePack.Key(3)]
        public double DoubleValue { get; set; }
    }
}
