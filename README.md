# MessagePackReader

simple and nongeneric MessagePack Reader for C#. 

※同じライブラリ名があることに気が付いたのでライブラリ名修正しました。

# References

MessagePackReaderはMiniJSON[1]とMessagePack-CSharp[2]を参考にしています

MessagePackReader is inspired by MiniJSON[1] and MessagePack-CSharp[2].

* [1] MiniJSON, https://gist.github.com/darktable/1411710
* [2] MessagePack-CSharp, https://github.com/neuecc/MessagePack-CSharp

# Concept

* .NET Framework 3.5
    * まだUnity/.NET3.5環境なユーザーがいるはずです（私
    * Some Unity users still need to use .NET3 ...(me too
* do not use unsafe(default)
    * do not force change unsafe setting of Unity.<br>
      but you can use #define UNSAFE_BYTEBUFFER, it has an impact on memory performance.
* MiniJSON Like
    * 単独ファイル
        * 導入が簡単
    * 読み取り時型引数不要
        * 大量の読み取り用型を生成すると、IL2CPPは大量のC++ファイル(数GB！)を生成し、ビルドエラーの原因となることがあります
    * Single File
        * Easy to install
    * Does not need types to deserialize
    	* Declare a lot of classes to deserialize, IL2CPP will generate huge C++ files(e.g. xGB) then will cause build error.
* Struct Base
    * なるべくヒープを経由しないようにします
    * avoid to use heap.
* byte[] to primitive.
    * ボクシング回避
        * 値型をobject型で読み取るとボクシングが発生するため
    * avoid boxing
    	* because deserialize from primitive to object, it will cause boxing.

# Warning

MessagePackReader can "not" serialize data.

If you want to do it, I recommend to use other libraries, like MsgPackCli, MessagePack-CSharp...

# QuickStart

* Copy src/MessagePackReader/MessagePack/MessagePackReader.cs to your project

```csharp
  byte[] msgpack;
  var reader = MessagePackReader.MessagePackReader.Create(msgpack);
  
  //simple pattern. easy to use but slow.
  {
    var length = reader.ArrayLength;
    for(int i = 0; i < length; i++){
      byte   b = reader[i]["ByteValue"].GetByte();
      string s = reader[i]["StringValue"].GetString();
    }
  }

  //foreach pattern. faster than for
  foreach(var arrayValue in reader.AsArrayEnumerable()){
    byte   b = arrayValue["ByteValue"].GetByte();
    string s = arrayValue["StringValue"].GetString();
  }

  //fastest pattern. reuse string key as utf8 byte[].
  {
    var byteKey = MessagePackReader.MessagePackReader.KeyToBytes("ByteValue");
    var strKey  = MessagePackReader.MessagePackReader.KeyToBytes("StringValue");
    foreach(var arrayValue in reader.AsArrayEnumerable()){
      byte   b = arrayValue[byteKey].GetByte();
      string s = arrayValue[strKey ].GetString();
    }
  }
```

# Profile

1000要素配列を走査しプロパティを参照。負荷が低すぎるような・・・

read array of 1000 instances and get properties. is it true...?

Flatbuffers and MessagePack-CSharp are crazy fast!


|                                  Method |        Mean | Allocated |
|---------------------------------------- |------------:|----------:|
|               Array_Flatbuffers         |    285.4 us |  19.65 KB |
|         Array_MessagePackCSharp         |    769.0 us |  50.83 KB |
|  Array_MessagePackCSharpKeyAttr         |    228.8 us |  50.83 KB |
| Array_MessagePackCSharpTypeless         |  1,176.5 us | 402.24 KB |
|                  Array_Utf8Json         |  1,790.7 us | 570.65 KB |
|                  Array_MiniJSON         |  2,561.5 us | 1107.3 KB |
|                Array_MsgPackCli         |    884.2 us |    176 KB |
|   Array_MiniMessagePackForeach1         |    675.0 us |  78.25 KB |
|   Array_MiniMessagePackForeach2         |    809.1 us | 191.71 KB |
|        Array_MiniMessagePackFor         | 86,676.5 us |  78.67 KB |
|   Array_MiniMessagePackBytesKey         |    567.0 us |  35.28 KB |
|   Array_MiniMessagePackBytesKey(unsafe) |    533.6 us |  19.63 KB |
