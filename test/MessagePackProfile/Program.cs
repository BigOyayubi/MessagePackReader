using System;
using BenchmarkDotNet.Running;

namespace MessagePackProfile
{
    public class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<SerializerBenchmark>();
        }
    }
}
