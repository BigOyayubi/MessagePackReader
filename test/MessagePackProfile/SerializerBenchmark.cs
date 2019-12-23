using System;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;

namespace MessagePackProfile
{
    [Config(typeof(BenchmarkConfig))]
    public class SerializerBenchmark
    {
        [Benchmark]
        public void Deserialize()
        {
            Console.WriteLine(1);
        }
    }
}
