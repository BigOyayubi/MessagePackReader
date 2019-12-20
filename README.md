g MiniMessagePack
simple and typeless MessagePack Reader for C#. 

# Concept

* .NET Framework 3.5
    * Some Unity users still need to use .NET3 ...(me too
* MiniJSON Like
    * Single File
    * Does not need types to deserialize
    	* Declare a lot of classes to deserialize, IL2CPP will generate huge C++ files(e.g. xGB) then will cause build error.
* Read Only
    * avoid boxing
    	* deserialize from primitive to object, it will cause boxing.
    * if you want to serialize some data to messagepack,<br>
      I recommend to use other MessagePack libs. like MesaagePack-Cli, MessagePack-CSharp

