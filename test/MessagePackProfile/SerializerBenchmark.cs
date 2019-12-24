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

        [GlobalSetup]
        public void Setup()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                MessagePack.Resolvers.ContractlessStandardResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
                );
            var options = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePack.MessagePackSerializer.DefaultOptions = options;

            IList<Models.SampleModel1> arrayObjects = Enumerable.Range(0, 1000).Select(_ => new Models.SampleModel1().Set(_)).ToList();
            this.ArrayModelSerialized = MessagePack.MessagePackSerializer.Serialize(arrayObjects);
            this.ArrayModelJsonString = MessagePack.MessagePackSerializer.SerializeToJson(arrayObjects);
        }

        [Benchmark]
        public void Array_MessagePackCSharp()
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
        public void Array_MessagePackCSharpTypeless()
        {
            var deserialize = MessagePack.MessagePackSerializer.Deserialize<object>(this.ArrayModelSerialized);
            var list = deserialize as object[];
            var length = list.Length;
            for(int i = 0; i < length; i++)
            {
                var value = list[i] as Dictionary<object, object>;
                var kv = value["Key"];
                var io = value["IntValue"];
                if(io is byte)
                {
                    var iv = (byte)io;
                }
                else if(io is ushort)
                {
                    var iv = (ushort)io;
                }
                var fv = (float)value["FloatValue"];
            }
        }

        [Benchmark]
        public void Array_Utf8Json()
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
        public void Array_MiniJSON()
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
        public void Array_MsgPackCli()
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

        [Benchmark]
        public void Array_MiniMessagePackForeach1()
        {
            var reader = Reader.Create(this.ArrayModelSerialized);

            //faster than below
            foreach (var arrayValue in reader.AsArrayEnumerable())
            {
                arrayValue["Key"].GetString();
                arrayValue["IntValue"].GetInt();
                arrayValue["FloatValue"].GetFloat();
            }
        }


        [Benchmark]
        public void Array_MiniMessagePackForeach2()
        {
            var reader = Reader.Create(this.ArrayModelSerialized);

            //faster than below
            foreach(var arrayValue in reader.AsArrayEnumerable())
            {
                foreach(var mapValue in arrayValue.AsMapEnumerable())
                {
                    switch(mapValue.Key)
                    {
                        case "Key":
                            var sv = mapValue.Value.GetString();
                            break;
                        case "IntValue":
                            var iv = mapValue.Value.GetInt();
                            break;
                        case "FloatValue":
                            var fv = mapValue.Value.GetFloat();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        [Benchmark]
        public void Array_MiniMessagePackFor()
        {
            var reader = Reader.Create(this.ArrayModelSerialized);

            var length = reader.ArrayLength;
            for (int i = 0; i < length; i++)
            {
                var tmp = reader[i];

                tmp["Key"].GetString();
                tmp["IntValue"].GetInt();
                tmp["FloatValue"].GetFloat();
            }
        }

        [Benchmark]
        public void Array_MiniMessagePackBytesKey()
        {
            var reader = Reader.Create(this.ArrayModelSerialized);

            var KeyBytes = Reader.ToBytes("Key");
            var IntValueBytes = Reader.ToBytes("IntValue");
            var FloatValueBytes = Reader.ToBytes("FloatValue");

            foreach (var arrayValue in reader.AsArrayEnumerable())
            {
                arrayValue[KeyBytes].GetString();
                arrayValue[IntValueBytes].GetInt();
                arrayValue[FloatValueBytes].GetFloat();
            }
        }
    }
}
