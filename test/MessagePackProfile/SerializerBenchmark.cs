using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using MiniMessagePack;

namespace MessagePackProfile
{
    [Config(typeof(BenchmarkConfig))]
    public class SerializerBenchmark
    {
        private byte[] ArrayModelSerialized;
        private string ArrayModelJsonString;
        private byte[] MapModelSerialized;

        [GlobalSetup]
        public void Setup()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                MessagePack.Resolvers.ContractlessStandardResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
                );
            var options = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePack.MessagePackSerializer.DefaultOptions = options;

            IList<Models.SampleModel1> arrayObjects = Enumerable.Range(0, 100).Select(_ => new Models.SampleModel1()).ToList();
            this.ArrayModelSerialized = MessagePack.MessagePackSerializer.Serialize(arrayObjects);
            this.ArrayModelJsonString = MessagePack.MessagePackSerializer.SerializeToJson(arrayObjects);
        }

        [Benchmark]
        public void ArrayMessagePackCSharp()
        {
            var deserialize = MessagePack.MessagePackSerializer.Deserialize<IList<Models.SampleModel1>>(this.ArrayModelSerialized);
            var length = deserialize.Count;
            for (int i = 0; i < length; i++)
            {
                var value = deserialize[i];
                var kv = value.Key;
                var iv = value.IntValue;
                var fv = value.FloatValue;
            }
        }

        [Benchmark]
        public void ArrayMessagePackCSharpTypeless()
        {
            var deserialize = MessagePack.MessagePackSerializer.Deserialize<object>(this.ArrayModelSerialized);
            var list = deserialize as object[];
            var length = list.Length;
            for(int i = 0; i < length; i++)
            {
                var value = list[i] as Dictionary<object, object>;
                var kv = value["Key"];
                var iv = (byte)value["IntValue"];
                var fv = (float)value["FloatValue"];
            }
        }

        [Benchmark]
        public void ArrayUtf8Json()
        {
            var deserialize = Utf8Json.JsonSerializer.Deserialize<dynamic>(this.ArrayModelJsonString) as List<object>;
            var length = deserialize.Count;
            for(int i = 0; i < length; i++)
            {
                var value = deserialize[i] as Dictionary<string, object>;
                var kv = value["Key"];
                var iv = (int)(double)value["IntValue"];
                var fv = (float)(double)value["FloatValue"];
            }
        }

        [Benchmark]
        public void ArrayMiniJSON()
        {
            var deserialize = MiniJSON.Json.Deserialize(this.ArrayModelJsonString) as IList<object>;
            var length = deserialize.Count;
            for(int i = 0; i < length; i++)
            {
                var value = deserialize[i] as IDictionary<string, object>;
                var kv = value["Key"];
                var iv = (int)(long)value["IntValue"];
                var fv = (float)(long)value["FloatValue"];
            }
        }

        [Benchmark]
        public void ArrayMsgPackCli()
        {
            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<IList<Models.SampleModel1>>();
            var deserialize = serializer.UnpackSingleObject(this.ArrayModelSerialized);
            var length = deserialize.Count;
            for(int i = 0; i < length; i++)
            {
                var value = deserialize[i];
                var kv = value.Key;
                var iv = value.IntValue;
                var fv = value.FloatValue;
            }
        }


        //[Benchmark]
        public object DeserializeMapModelSerializedMessagePackCSharp() => MessagePack.MessagePackSerializer.Deserialize<dynamic>(this.MapModelSerialized);

        [Benchmark]
        public void ArrayModelMiniMessagePack()
        {
            var reader = Reader.Deserialize(this.ArrayModelSerialized);
            var length = reader.ArrayLength;
            for(int i = 0; i < length; i++)
            {
                var tmp = reader[i];

                tmp["Key"].GetString();
                tmp["IntValue"].GetInt();
                tmp["FloatValue"].GetFloat();
            }
        }
    }
}
