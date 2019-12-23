using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackProfile.Models
{
    [MessagePack.MessagePackObject(keyAsPropertyName: true)]
    public class SampleModel1
    {
        public string Key { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
    }
}
