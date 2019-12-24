# MiniMessagePack

simple and typeless MessagePack Reader for C#. 

# References

MiniMessagePackはMiniJSON[1]とMessagePack-CSharp[2]を参考にしています

MiniMessagePack is inspired by MiniJSON[1] and MessagePack-CSharp[2].

* [1] MiniJSON, https://gist.github.com/darktable/1411710
* [2] MessagePack-CSharp, https://github.com/neuecc/MessagePack-CSharp

# Concept

* .NET Framework 3.5
    * まだUnity/.NET3.5環境なユーザーがいるはずです（私
    * Some Unity users still need to use .NET3 ...(me too
* MiniJSON Like
    * 単独ファイル
        * 導入が簡単
    * 読み取り時型引数不要
        * 大量の読み取り用型を生成すると、IL2CPPは大量のC++ファイル(数GB！)を生成し、ビルドエラーの原因となることがあります
    * Single File
        * Easy to install
    * Does not need types to deserialize
    	* Declare a lot of classes to deserialize, IL2CPP will generate huge C++ files(e.g. xGB) then will cause build error.
* Read Only
    * ボクシング回避
        * 値型をobject型で読み取るとボクシングが発生するため
    * データをシリアライズしたい場合、他のMessagePackライブラリを使うことをおすすめします。
    * avoid boxing
    	* because deserialize from primitive to object, it will cause boxing.
    * if you want to serialize some data to messagepack,<br>
      I recommend to use other MessagePack libs. like MesaagePack-Cli, MessagePack-CSharp

# QuickStart

* Copy src/MiniMessagePack/MessagePack/MiniMessagePack.cs to your project

```
  byte[] msgpack;
  var reader = MiniMessagePack.MsgPack.Deserialize(msgpack);
  byte b = reader["a"][0].GetByte();
  string s = reader["b"].GetString();
```

# Profile

1000要素配列を走査しプロパティを参照。負荷が低すぎるような・・・
read array of 1000 instances and get properties. is it true...?

![profile](https://github.com/BigOyayubi/MiniMessagePack/blob/master/doc/profile.jpg)
