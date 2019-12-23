using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Columns;


namespace MessagePackProfile
{
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            //Job baseConfig = Job.ShortRun.WithIterationCount(1).WithWarmupCount(1);

            //this.Add(baseConfig.With(CoreRuntime.Core30).With(Jit.RyuJit).With(Platform.X64));

            this.Add(MarkdownExporter.GitHub);
            this.Add(CsvExporter.Default);
            this.Add(MemoryDiagnoser.Default);
        }
    }
}
