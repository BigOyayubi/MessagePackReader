using System;
using System.Collections;
using System.Collections.Generic;

namespace MiniMessagePack
{
    public partial struct Reader
    {
        #region public
        /// <summary>
        /// MsgPack参照準備
        /// Prepare to read MessagePack
        /// </summary>
        public static Reader Deserialize(byte[] data)
        {
            if (data == null || data.Length <= 0)
            {
                throw new System.NullReferenceException();
            }

            return new Reader(data);
        }

        /// <summary>
        /// Mapであればkeyに該当するValueを指すMsgPackを取得
        /// Get MsgPack which peek value of key.
        /// </summary>
        public Reader this[string key]
        {
            get
            {
                var binkey = System.Text.Encoding.UTF8.GetBytes(key);
                int mapValuePosition = _reader.GetMapValuePosition(binkey);
                return new Reader(_reader.Source, mapValuePosition);
            }
        }

        public int ArrayLength
        {
            get
            {
                return _reader.GetArrayLength();
            }
        }

        /// <summary>
        /// Arrayであればindexに該当するValueを指すMsgPackを取得
        /// Get MsgPack which peek value of index.
        /// </summary>
        public Reader this[int index]
        {
            get
            {
                int arrayElementPosition = _reader.GetArrayElementPosition(index);
                return new Reader(_reader.Source, arrayElementPosition);
            }
        }

        public byte GetByte() { return _reader.GetByte(); }
        public sbyte GetSByte() { return _reader.GetSByte(); }
        public short GetShort() { return _reader.GetShort(); }
        public ushort GetUShort() { return _reader.GetUShort(); }
        public int GetInt() { return _reader.GetInt(); }
        public uint GetUInt() { return _reader.GetUInt(); }
        public long GetLong() { return _reader.GetLong(); }
        public ulong GetULong() { return _reader.GetULong(); }
        public float GetFloat() { return _reader.GetFloat(); }
        public double GetDouble() { return _reader.GetDouble(); }
        public bool GetBool() { return _reader.GetBool(); }
        public string GetString() { return _reader.GetString(); }
        public byte[] GetBinary() { return _reader.GetBinary(); }
        public DateTime GetTimestamp() { return _reader.GetTimestamp(); }
        public Extension GetExtension() { return _reader.GetExtension(); }
        public byte GetFormatCode() { return _reader.Source[_reader.Position]; }
        public string GetFormatName() { return Spec.GetFormatName(GetFormatCode()); }
        #endregion //public
    }

    public class MiniMessagePackException : System.Exception
    {
        public MiniMessagePackException() { }
        public MiniMessagePackException(string message) : base(message) { }
    }

    /// <summary>
    /// messagePackPack Extension data
    /// </summary>
    public struct Extension
    {
        public sbyte TypeCode;
        public byte[] Data;
    }

    public partial struct Reader
    {
        readonly MsgpackReader _reader;
        Reader(byte[] data, int position = 0)
        {
            _reader = new MsgpackReader(data, position);
        }

        struct MsgpackReader
        {
            public MsgpackReader(byte[] data, int position = 0)
            {
                _source = data;
                _position = position;
            }

            public void Reset(byte[] data, int position)
            {
                _source = data;
                _position = position;
            }

            public byte[] Source { get { return _source; } }

            public int Position { get { return _position; } }

            /// <summary>
            /// Seek位置がArrayであれば、Element数を返す
            /// </summary>
            /// <returns></returns>
            public int GetArrayLength()
            {
                var reader = new SequencialReader(_source, _position);
                return reader.ReadArrayElementCount();
            }

            /// <summary>
            /// Seek位置がArrayであれば、該当indexのElement位置を返す
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public int GetArrayElementPosition(int index)
            {
                var reader = new SequencialReader(_source, _position);

                int arrayElementCount = reader.ReadArrayElementCount();

                if(index > arrayElementCount)
                {
                    throw new MiniMessagePackException(string.Format("invalid array index. index : {0}", index));
                }

                for(int i = 0; i < index; i++)
                {
                    reader.SkipElement();
                }

                return reader.Position;
            }

            /// <summary>
            /// Seek位置がMapであれば、該当KeyのValue位置を返す
            /// </summary>
            public int GetMapValuePosition(string key)
            {
                var reader = new SequencialReader(_source, _position);

                int mapElementCount = reader.ReadMapElementCount();

                for(int i = 0; i < mapElementCount; i++)
                {
                    //get key
                    string mapkey = reader.ReadString();
                    if(mapkey == key)
                    {
                        return reader.Position;
                    }
                    //skip value
                    reader.SkipElement();
                }

                // not found key
                throw new MiniMessagePackException(string.Format("not found key {0}", key));
            }
            public int GetMapValuePosition(byte[] key)
            {
                var reader = new SequencialReader(_source, _position);

                int mapElementCount = reader.ReadMapElementCount();
                for(int i = 0; i < mapElementCount; i++)
                {
                    if(reader.CompareAndReadString(key))
                    {
                        return reader.Position;
                    }
                    reader.SkipElement();
                }
                // not found key
                throw new MiniMessagePackException(string.Format("not found key {0}", key));
            }

            public byte GetByte()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch(token)
                {
                    case Spec.Fmt_Uint8:
                        return reader.ReadByte();
                    default:
                        if(Spec.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MiniMessagePackException(string.Format("failed to read byte. code : {0:X}", token));
                }
            }

            public sbyte GetSByte()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch(token)
                {
                    case Spec.Fmt_Uint8:
                        byte byteResult = reader.ReadByte();
                        return unchecked((sbyte)byteResult);
                    case Spec.Fmt_Int8:
                        return reader.ReadSByte();
                    default:
                        if(Spec.IsPositiveFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        else if(Spec.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MiniMessagePackException(string.Format("failed to read sbyte. code : {0:X}", token));
                }
            }

            public short GetShort()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch(token)
                {
                    case Spec.Fmt_Uint8:
                        return reader.ReadByte();
                    case Spec.Fmt_Int8:
                        return reader.ReadSByte();
                    case Spec.Fmt_Int16:
                        return reader.ReadShort();
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        else if (Spec.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MiniMessagePackException(string.Format("failed to read short. code : {0:X}", token));
                }
            }

            public ushort GetUShort()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch(token)
                {
                    case Spec.Fmt_Uint8:
                        return reader.ReadByte();
                    case Spec.Fmt_Uint16:
                        return reader.ReadUShort();
                    default:
                        if(Spec.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MiniMessagePackException(string.Format("failed to read ushort. code : {0}", token));
                }
            }

            public int GetInt()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch(token)
                {
                    case Spec.Fmt_Uint8:
                        return reader.ReadByte();
                    case Spec.Fmt_Int8:
                        return reader.ReadSByte();
                    case Spec.Fmt_Uint16:
                        return reader.ReadUShort();
                    case Spec.Fmt_Int16:
                        return reader.ReadShort();
                    case Spec.Fmt_Int32:
                        return reader.ReadInt();
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        else if (Spec.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MiniMessagePackException(string.Format("failed to read int. code : {0}", token));
                }
            }

            public uint GetUInt()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        return reader.ReadByte();
                    case Spec.Fmt_Uint16:
                        return reader.ReadUShort();
                    case Spec.Fmt_Uint32:
                        return reader.ReadUInt();
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MiniMessagePackException(string.Format("failed to read uint. code : {0:X}", token));
                }
            }

            public long GetLong()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        return reader.ReadByte();
                    case Spec.Fmt_Int8:
                        return reader.ReadSByte();
                    case Spec.Fmt_Uint16:
                        return reader.ReadUShort();
                    case Spec.Fmt_Int16:
                        return reader.ReadShort();
                    case Spec.Fmt_Int32:
                        return reader.ReadInt();
                    case Spec.Fmt_Uint32:
                        return reader.ReadUInt();
                    case Spec.Fmt_Int64:
                        return reader.ReadLong();
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        else if (Spec.IsNegativeFixInt(token))
                        {
                            return unchecked((sbyte)token);
                        }
                        throw new MiniMessagePackException(string.Format("failed to read long. code : {0:X}", token));
                }
            }

            public ulong GetULong()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        return reader.ReadByte();
                    case Spec.Fmt_Uint16:
                        return reader.ReadUShort();
                    case Spec.Fmt_Uint32:
                        return reader.ReadUInt();
                    case Spec.Fmt_Uint64:
                        return reader.ReadULong();
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            return token;
                        }
                        throw new MiniMessagePackException(string.Format("failed to read ulong. code : {0:X}", token));
                }
            }

            public float GetFloat()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();
                if (!Spec.IsFloat(token))
                {
                    throw new MiniMessagePackException(string.Format("failed to read float. code : {0}", token));
                }
                return reader.ReadFloat();
            }

            public double GetDouble()
            {
                var reader = new SequencialReader(_source, _position);

                var token = reader.ReadToken();

                switch(token)
                {
                    case Spec.Fmt_Float32:
                        return reader.ReadFloat();
                    case Spec.Fmt_Float64:
                        return reader.ReadDouble();
                    default:
                        throw new MiniMessagePackException(string.Format("failed to read double. code : {0:X}", token));

                }
            }

            public Nil GetNil()
            {
                return new SequencialReader(_source, _position).ReadNil();
            }

            public bool GetBool()
            {
                return new SequencialReader(_source, _position).ReadBoolean();
            }

            public string GetString()
            {
                return new SequencialReader(_source, _position).ReadString();
            }

            public byte[] GetBinary()
            {
                return new SequencialReader(_source, _position).ReadBinary();
            }

            public DateTime GetTimestamp()
            {
                var reader = new SequencialReader(_source, _position);
                var header = reader.ReadExtHeader();
                return reader.ReadTimestamp(header);
            }

            public Extension GetExtension()
            {
                var reader = new SequencialReader(_source, _position);
                var header = reader.ReadExtHeader();
                var data = reader.ReadExtensionData(header);
                return new Extension() { TypeCode = header.TypeCode, Data = data };
            }

            byte[] _source;
            int _position;
        }

        struct SequencialReader
        {
            readonly byte[] _source;
            int _position;

            public SequencialReader(byte[] source, int position)
            {
                _source = source;
                _position = position;
            }

            public int Position { get { return _position;  } }

            public byte PeekToken
            {
                get
                {
                    return _source[_position];
                }
            }

            public byte PeekSourceType
            {
                get
                {
                    return Spec.GetSourceType(PeekToken);
                }
            }

            public string PeekFormatName
            {
                get
                {
                    return Spec.GetFormatName(PeekToken);
                }
            }

            public void SkipElement()
            {
                var token = PeekToken;
                var sourceType = Spec.GetSourceType(token);

                switch(sourceType)
                {
                    case Spec.Type_Integer:
                        token = ReadToken();
                        if(Spec.IsNegativeFixInt(token) || Spec.IsPositiveFixInt(token))
                        {
                            //nothing to do
                            return;
                        }
                        else if(token == Spec.Fmt_Int8 || token == Spec.Fmt_Uint8)
                        {
                            _position += sizeof(byte);
//                            ReadSByte();
                            return;
                        }
                        else if(token == Spec.Fmt_Int16 || token == Spec.Fmt_Uint16)
                        {
                            _position += sizeof(short);
//                            ReadShort();
                            return;
                        }
                        else if(token == Spec.Fmt_Int32 || token == Spec.Fmt_Uint32)
                        {
                            _position += sizeof(int);
//                            ReadInt();
                            return;
                        }
                        else if(token == Spec.Fmt_Int64 || token == Spec.Fmt_Uint64)
                        {
                            _position += sizeof(long);
//                            ReadLong();
                            return;
                        }

                        throw new MiniMessagePackException("Invalid primitive bytes.");
                    case Spec.Type_Boolean:
                        token = ReadToken();
//                        ReadBoolean();
                        break;
                    case Spec.Type_Float:
                        token = ReadToken();
                        if(token == Spec.Fmt_Float32)
                        {
                            _position += sizeof(float);
//                            ReadFloat();
                            return;
                        }
                        else
                        {
                            _position += sizeof(double);
//                            ReadDouble();
                            return;
                        }

                        throw new MiniMessagePackException("Invalid primitive bytes.");
                    case Spec.Type_String:
                        var stringLength = ReadStringByteLength();
                        _position += stringLength;
//                        ReadString();
                        return;
                    case Spec.Type_Binary:
                        var byteLength = ReadBinaryByteLength();
                        _position += byteLength;
//                        ReadBinary();
                        return;
                    case Spec.Type_Extension:
                        var extHeader = ReadExtHeader();
                        _position += (int)extHeader.Length;
//                        if(extHeader.TypeCode == Spec.ExtTypeCode_Timestamp)
//                        {
//                            ReadTimestamp(extHeader);
//                            return;
//                        }
                        throw new MiniMessagePackException("Invalid primitive bytes.");
                    case Spec.Type_Array:
                        var arrayElementCount = ReadArrayElementCount();
                        for(int i = 0; i < arrayElementCount; i++)
                        {
                            SkipElement();
                        }
                        return;
                    case Spec.Type_Map:
                        var mapElementCount = ReadMapElementCount();
                        for(int i = 0; i < mapElementCount; i++)
                        {
//                            ReadString();
                            SkipElement();//key
                            SkipElement();//value
                        }
                        return;
                    case Spec.Type_Nil:
                        ReadNil();
                        return;
                    default:
                        throw new MiniMessagePackException("Invalid primitive bytes.");
                }
            }

            public Nil ReadNil()
            {
                byte code;
                ThrowEndOfStreamExceptionUnless(TryRead(out code));
                if (!Spec.IsNil(code))
                {
                    ThrowInvalidCodeException(code);
                }
                return Nil.Default;
            }

            public int ReadArrayElementCount()
            {
                int v;
                ThrowEndOfStreamExceptionUnless(TryReadArrayElementCount(out v));
                return v;
            }

            public bool TryReadArrayElementCount(out int count)
            {
                byte token;
                if(!TryReadToken(out token))
                {
                    count = default(int);
                    return false;
                }

                switch(token)
                {
                    case Spec.Fmt_Array16:
                        ushort ushortResult;
                        if(TryReadBigEndian(out ushortResult))
                        {
                            count = ushortResult;
                            return true;
                        }
                        break;
                    case Spec.Fmt_Array32:
                        uint uintResult;
                        if(TryReadBigEndian(out uintResult))
                        {
                            count = checked((int)uintResult);
                            return true;
                        }
                        break;
                    default:
                        if(Spec.IsFixArray(token))
                        {
                            count = checked((byte)(token & 0xf));
                            return true;
                        }
                        ThrowInvalidCodeException(token);
                        break;
                }

                count = default(int);
                return false;
            }

            public int ReadMapElementCount()
            {
                int v;
                ThrowEndOfStreamExceptionUnless(TryReadMapElementCount(out v));
                return v;
            }

            public bool TryReadMapElementCount(out int count)
            {
                count = 0;

                byte token;
                if (!TryReadToken(out token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Map16:
                        {
                            ushort value;
                            if (!TryRead(out value)) return false;
                            count = checked((int)value);
                        }
                        break;
                    case Spec.Fmt_Map32:
                        {
                            uint value;
                            if (!TryRead(out value)) return false;
                            count = checked((int)value);
                        }
                        break;
                    default:
                        if (Spec.Fmt_MinFixMap <= token && token <= Spec.Fmt_MaxFixMap)
                        {
                            count = checked((int)(token & 0xf));
                            break;
                        }
                        ThrowInvalidCodeException(token);
                        break;
                }

                return true;
            }

            public DateTime ReadTimestamp(ExtensionHeader header)
            {
                if(header.TypeCode != Spec.ExtTypeCode_Timestamp)
                {
                    throw new MiniMessagePackException(string.Format("Invalid Extension TypeCode {0}", header.TypeCode));
                }

                switch(header.Length)
                {
                    case 4:
                        {
                            uint uintResult;
                            ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out uintResult));
                            return DateTimeConverter.GetDateTime(uintResult);
                        }
                    case 8:
                        {
                            ulong ulongResult;
                            ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out ulongResult));
                            uint nanoseconds = (uint)(ulongResult >> 34);
                            uint seconds = (uint)(ulongResult & 0x00000003ffffffffL);
                            return DateTimeConverter.GetDateTime(seconds, nanoseconds);
                        }
                    case 12:
                        {
                            uint uintResult;
                            ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out uintResult));
                            long longResult;
                            ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out longResult));
                            return DateTimeConverter.GetDateTime(longResult, uintResult);
                        }
                    default:
                        throw new MiniMessagePackException(string.Format("Invalid Ext Timestamp length {0}", header.Length));
                }
            }
            public byte[] ReadExtensionData(ExtensionHeader header)
            {
                byte[] data = new byte[header.Length];
                Array.Copy(_source, _position, data, 0, header.Length);
                return data;
            }

            public ExtensionHeader ReadExtHeader()
            {
                var token = ReadToken();
                uint length = default(uint);
                switch(token)
                {
                    case Spec.Fmt_FixExt1:
                        length = 1;
                        break;
                    case Spec.Fmt_FixExt2:
                        length = 2;
                        break;
                    case Spec.Fmt_FixExt4:
                        length = 4;
                        break;
                    case Spec.Fmt_FixExt8:
                        length = 8;
                        break;
                    case Spec.Fmt_FixExt16:
                        length = 16;
                        break;
                    case Spec.Fmt_Ext8:
                        byte byteResult;
                        ThrowEndOfStreamExceptionUnless(TryRead(out byteResult));
                        length = byteResult;
                        break;
                    case Spec.Fmt_Ext16:
                        short shortResult;
                        ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out shortResult));
                        length = checked((uint)shortResult);
                        break;
                    case Spec.Fmt_Ext32:
                        int intResult;
                        ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out intResult));
                        length = checked((uint)intResult);
                        break;
                    default:
                        ThrowInvalidCodeException(token);
                        break;
                }

                sbyte typeCode;
                ThrowEndOfStreamExceptionUnless(TryRead(out typeCode));

                return new ExtensionHeader(typeCode, length);
            }

            public byte[] ReadBinary()
            {
                var byteLength = ReadBinaryByteLength();

                byte[] bytesResult = new byte[byteLength];
                Array.Copy(_source, _position, bytesResult, 0, byteLength);

                return bytesResult;
            }

            int ReadBinaryByteLength()
            {
                var token = ReadToken();
                switch(token)
                {
                    case Spec.Fmt_Bin8:
                        byte byteResult;
                        ThrowEndOfStreamExceptionUnless(TryRead(out byteResult));
                        return checked((int)byteResult);
                    case Spec.Fmt_Bin16:
                        ushort ushortResult;
                        ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out ushortResult));
                        return checked((int)ushortResult);
                    case Spec.Fmt_Bin32:
                        uint uintResult;
                        ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out uintResult));
                        return checked((int)uintResult);
                    default:
                        ThrowInvalidCodeException(token);
                        break;
                }

                return default(int);
            }

            public bool CompareAndReadString(byte[] key)
            {
                var token = PeekToken;
                if (token == Spec.Fmt_Nil)
                {
                    return false;
                }

                var byteLength = ReadStringByteLength();
                if(byteLength != key.Length)
                {
                    _position += byteLength;
                    return false;
                }
                for(int i = 0; i < byteLength; i++)
                {
                    if(_source[_position+i] != key[i])
                    {
                        _position += byteLength;
                        return false;
                    }
                }

                _position += byteLength;
                return true;
            }

            public string ReadString()
            {
                var token = PeekToken;
                if(token == Spec.Fmt_Nil)
                {
                    return "";
                }

                var byteLength = ReadStringByteLength();

                var value = System.Text.Encoding.UTF8.GetString(_source, _position, byteLength);
                _position += byteLength;
                return value;
            }

            int ReadStringByteLength()
            {
                var token = ReadToken();
                switch (token)
                {
                    case Spec.Fmt_Str8:
                        byte byteResult;
                        ThrowEndOfStreamExceptionUnless(TryRead(out byteResult));
                        return checked((int)byteResult);
                    case Spec.Fmt_Str16:
                        short shortResult;
                        ThrowEndOfStreamExceptionUnless(TryRead(out shortResult));
                        return checked((int)shortResult);
                    case Spec.Fmt_Str32:
                        int intResult;
                        ThrowEndOfStreamExceptionUnless(TryRead(out intResult));
                        return intResult;
                    default:
                        if (Spec.Fmt_MinFixStr <= token && token <= Spec.Fmt_MaxFixStr)
                        {
                            return (token & 0x1f);
                        }
                        ThrowInvalidCodeException(token);
                        break;
                }

                return default(int);
            }

            public bool ReadBoolean()
            {
                var token = ReadToken();

                switch(token)
                {
                    case Spec.Fmt_False:
                        return false;
                    case Spec.Fmt_True:
                        return true;
                    default:
                        throw ThrowInvalidCodeException(token);
                }
            }

            public byte ReadByte()
            {
                byte value;
                ThrowEndOfStreamExceptionUnless(TryRead(out value));
                return value;
            }
            public sbyte ReadSByte()
            {
                sbyte value;
                ThrowEndOfStreamExceptionUnless(TryRead(out value));
                return value;
            }

            public short ReadShort()
            {
                short value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public ushort ReadUShort()
            {
                ushort value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public int ReadInt()
            {
                int value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public uint ReadUInt()
            {
                uint value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public long ReadLong()
            {
                long value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public ulong ReadULong()
            {
                ulong value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public float ReadFloat()
            {
                float value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public double ReadDouble()
            {
                double value;
                ThrowEndOfStreamExceptionUnless(TryReadBigEndian(out value));
                return value;
            }

            public bool TryRead(out byte value)
            {
                if ((_source.Length - _position) < sizeof(byte))
                {
                    value = default(byte);
                    return false;
                }

                value = _source[_position];
                _position += sizeof(byte);
                return true;
            }

            public bool TryReadBigEndian(out float value)
            {
                if(!BitConverter.IsLittleEndian)
                {
                    return TryRead(out value);
                }

                return TryReadReverseBit(out value);
            }

            public bool TryReadBigEndian(out double value)
            {
                if(!BitConverter.IsLittleEndian)
                {
                    return TryRead(out value);
                }

                return TryReadReverseBit(out value);
            }

            public bool TryReadBigEndian(out short value)
            {
                if(!BitConverter.IsLittleEndian)
                {
                    return TryRead(out value);
                }

                return TryReadReverseBit(out value);
            }

            public bool TryReadBigEndian(out ushort value)
            {
                short tmp;
                if(!TryReadBigEndian(out tmp))
                {
                    value = default(ushort);
                    return false;
                }
                value = unchecked((ushort)tmp);
                return true;
            }

            public bool TryReadBigEndian(out int value)
            {
                if(!BitConverter.IsLittleEndian)
                {
                    return TryRead(out value);
                }

                return TryReadReverseBit(out value);
            }

            public bool TryReadBigEndian(out uint value)
            {
                int tmp;
                if(!TryReadBigEndian(out tmp))
                {
                    value = default(uint);
                    return false;
                }

                value = unchecked((uint)tmp);
                return true;
            }

            public bool TryReadBigEndian(out long value)
            {
                if(!BitConverter.IsLittleEndian)
                {
                    return TryRead(out value);
                }

                return TryReadReverseBit(out value);
            }

            public bool TryReadBigEndian(out ulong value)
            {
                long tmp;
                if(!TryReadBigEndian(out tmp))
                {
                    value = default(ulong);
                    return false;
                }

                value = unchecked((ulong)tmp);
                return true;
            }


            public byte ReadToken()
            {
                byte r;
                ThrowEndOfStreamExceptionUnless(TryReadToken(out r));

                return r;
            }

            public bool TryReadToken(out byte token)
            {
                if (_position == _source.Length)
                {
                    token = _source[_position - 1];
                    return false;
                }
                token = _source[_position];
                _position++;
                return true;
            }

            public bool TryReadSourceType(out byte type)
            {
                byte token;
                var r = TryReadToken(out token);
                type = Spec.GetSourceType(token);
                return r;
            }

            #region private

            bool TryReadReverseBit(out short value)
            {
                if(TryRead(out value))
                {
                    value = BitReverse(value);
                    return true;
                }
                value = default(short);
                return false;
            }

            bool TryReadReverseBit(out ushort value)
            {
                short tmp;
                if(TryRead(out tmp))
                {
                    value = unchecked((ushort)BitReverse(tmp));
                    return true;
                }
                value = default(ushort);
                return false;
            }

            bool TryReadReverseBit(out int value)
            {
                if(TryRead(out value))
                {
                    value = BitReverse(value);
                    return true;
                }
                value = default(int);
                return false;
            }

            bool TryReadReverseBit(out uint value)
            {
                int tmp;
                if(TryRead(out tmp))
                {
                    value = unchecked((uint)BitReverse(tmp));
                    return true;
                }

                value = default(uint);
                return false;
            }

            bool TryReadReverseBit(out long value)
            {
                if(TryRead(out value))
                {
                    value = BitReverse(value);
                    return true;
                }

                value = default(long);
                return false;
            }

            bool TryReadReverseBit(out ulong value)
            {
                long tmp;
                if(TryRead(out tmp))
                {
                    value = unchecked((ulong)BitReverse(tmp));
                    return true;
                }

                value = default(ulong);
                return false;
            }

            bool TryReadReverseBit(out float value)
            {
                //いい方法が思いついていない
                int tmp;
                if(TryRead(out tmp))
                {
                    var bytes = BitConverter.GetBytes(tmp);
                    ByteSwap(bytes, 0, 3);
                    ByteSwap(bytes, 1, 2);
                    value = BitConverter.ToSingle(bytes, 0);
                    return true;
                }

                value = default(float);
                return false;
            }

            bool TryReadReverseBit(out double value)
            {
                long tmp;
                if(TryRead(out tmp))
                {
#if false
                    //float.MinValueが渡されると失敗する
                    value = BitConverter.Int64BitsToDouble(BitReverse(tmp));
#else
                    var bytes = BitConverter.GetBytes(tmp);
                    ByteSwap(bytes, 0, 7);
                    ByteSwap(bytes, 1, 6);
                    ByteSwap(bytes, 2, 5);
                    ByteSwap(bytes, 3, 4);
                    value = BitConverter.ToDouble(bytes, 0);
#endif
                    return true;
                }
                value = default(double);
                return false;
            }

            bool TryRead<T>(out T value)
            {
                var typeSize = TypeSizeProvider.Instance.Get<T>();
                if ((_source.Length - _position) < typeSize)
                {
                    value = default(T);
                    return false;
                }

                value = BytesConverterResolver.Instance.GetConverter<T>().To(_source, _position);
                _position += typeSize;
                return true;
            }

            void ByteSwap(byte[] data, int idx1, int idx2)
            {
                byte tmp = data[idx1];
                data[idx1] = data[idx2];
                data[idx2] = tmp;
            }

            short BitReverse(short value)
            {
                return unchecked(
                    (short)(
                        ((value & 0xff) << 8) |
                        ((value >> 8) & 0xff)
                    )
                );
            }

            int BitReverse(int value)
            {
                return unchecked(
                    (int)(
                        ((value & 0xff)         << 24) |
                        (((value >>  8) & 0xff) << 16) |
                        (((value >> 16) & 0xff) <<  8) |
                         ((value >> 24) & 0xff)
                    )
                );
            }

            long BitReverse(long value)
            {
                return unchecked(
                    (long)(
                        ((value & 0xff) << 56) |
                        (((value >> 8) & 0xff) << 48) |
                        (((value >> 16) & 0xff) << 40) |
                        (((value >> 24) & 0xff) << 32) |
                        (((value >> 34) & 0xff) << 24) |
                        (((value >> 40) & 0xff) << 16) |
                        (((value >> 48) & 0xff) << 8) |
                         ((value >> 56) & 0xff)
                    )
                );
            }

            MiniMessagePackException ThrowInvalidCodeException(byte code)
            {
                throw new MiniMessagePackException(string.Format("Invalid code 0x{0:X}", code));
            }

            System.IO.EndOfStreamException ThrowEndOfStreamException()
            {
                throw new System.IO.EndOfStreamException();
            }

            void ThrowEndOfStreamExceptionUnless(bool cond)
            {
                if (!cond)
                {
                    ThrowEndOfStreamException();
                }
            }
#endregion //private
        }

        struct ExtensionHeader
        {
            public sbyte TypeCode { get; private set; }
            public uint Length { get; private set; }

            public ExtensionHeader(sbyte typeCode, uint length)
            {
                TypeCode = typeCode;
                Length = length;
            }

            public ExtensionHeader(sbyte typeCode, int length)
            {
                TypeCode = typeCode;
                Length = (uint)length;
            }
        }

        static class DateTimeConverter
        {
            static readonly DateTime _baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            public static DateTime GetDateTime(uint seconds)
            {
                return _baseTime.AddSeconds(seconds);
            }
            public static DateTime GetDateTime(uint seconds, uint nanoseconds)
            {
                return _baseTime.AddSeconds(seconds).AddTicks(nano2tick(nanoseconds));
            }
            public static DateTime GetDateTime(long seconds, uint nanoseconds)
            {
                return _baseTime.AddSeconds(seconds).AddTicks(nano2tick(nanoseconds));
            }
            static uint nano2tick(uint nanoseconds)
            {
                return nanoseconds / 100;
            }
        }

        /// <summary>
        /// primitive型のサイズを返す
        /// provide size of type
        /// </summary>
        class TypeSizeProvider
        {
            public static readonly TypeSizeProvider Instance = new TypeSizeProvider();
            public int Get<T>()
            {
                return Cache<T>.Size;
            }

            TypeSizeProvider() { }

            static class Cache<T>
            {
                public static readonly int Size;
                static Cache()
                {
                    Size = TypeSizeProviderCacheHelper.ToSize(typeof(T));
                }
            }

            static class TypeSizeProviderCacheHelper
            {
                public static int ToSize(Type type) { return TypeSizeMap[type]; }

                static readonly Dictionary<Type, int> TypeSizeMap = new Dictionary<Type, int>()
                {
                    { typeof(byte), sizeof(byte) },
                    { typeof(sbyte), sizeof(sbyte) },
                    { typeof(short), sizeof(short) },
                    { typeof(ushort), sizeof(ushort) },
                    { typeof(int), sizeof(int) },
                    { typeof(uint), sizeof(uint) },
                    { typeof(long), sizeof(long) },
                    { typeof(ulong), sizeof(ulong) },
                    { typeof(float), sizeof(float) },
                    { typeof(double), sizeof(double) },
                };
            }
        }

        /// <summary>
        /// 型引数に応じたbyte列->型変換処理
        /// </summary>
        interface IBytesConverter<T>
        {
            T To(byte[] data, int startIndex);
        }
        class BytesConverterResolver
        {
            public static readonly BytesConverterResolver Instance = new BytesConverterResolver();
            public IBytesConverter<T> GetConverter<T>()
            {
                return Cache<T>.converter;
            }

            BytesConverterResolver() { }

            static class Cache<T>
            {
                public static readonly IBytesConverter<T> converter;
                static Cache()
                {
                    converter = (IBytesConverter<T>)BytesConverterResolverCacheHelper.ToConverter(typeof(T));
                }
            }

            static class BytesConverterResolverCacheHelper
            {
                public static object ToConverter(Type type) { return ConverterMap[type]; }

                static readonly Dictionary<Type, object> ConverterMap = new Dictionary<Type, object>()
                {
                    { typeof(byte), new ByteConverter() },
                    { typeof(sbyte), new SByteConverter() },
                    { typeof(short), new ShortConverter() },
                    { typeof(ushort), new UShortConverter() },
                    { typeof(int), new IntConverter() },
                    { typeof(uint), new UIntConverter() },
                    { typeof(long), new LongConverter() },
                    { typeof(ulong), new ULongConverter() },
                    { typeof(float), new FloatConverter() },
                    { typeof(double), new DoubleConverter() },
                };
            }

            class ByteConverter  : IBytesConverter<byte>  { public byte To(byte[] data, int startIndex) { return data[startIndex]; } }
            class SByteConverter : IBytesConverter<sbyte> { public sbyte To(byte[] data, int startIndex){ return unchecked((sbyte)data[startIndex]); } }
            class ShortConverter : IBytesConverter<short> { public short To(byte[] data, int startIndex){ return BitConverter.ToInt16(data, startIndex); } }
            class UShortConverter : IBytesConverter<ushort> { public ushort To(byte[] data, int startIndex) { return BitConverter.ToUInt16(data, startIndex); } }
            class IntConverter : IBytesConverter<int> { public int To(byte[] data, int startIndex) { return BitConverter.ToInt32(data, startIndex); } }
            class UIntConverter : IBytesConverter<uint> { public uint To(byte[] data, int startIndex) { return BitConverter.ToUInt32(data, startIndex); } }
            class LongConverter : IBytesConverter<long> { public long To(byte[] data, int startIndex) { return BitConverter.ToInt64(data, startIndex); } }
            class ULongConverter : IBytesConverter<ulong> { public ulong To(byte[] data, int startIndex) { return BitConverter.ToUInt64(data, startIndex); } }
            class FloatConverter : IBytesConverter<float> { public float To(byte[] data, int startIndex) { return BitConverter.ToSingle(data, startIndex); } }
            class DoubleConverter : IBytesConverter<double> { public double To(byte[] data, int startIndex) { return BitConverter.ToDouble(data, startIndex); } }
        }



        struct Nil : IEquatable<Nil>
        {
            public static readonly Nil Default = default(Nil);

            public override bool Equals(object obj)
            {
                return obj is Nil;
            }

            public bool Equals(Nil other)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 0;
            }

            public override string ToString()
            {
                return "()";
            }
        }

        static class Spec
        {
#region Output formats
            public const byte Fmt_MinPositiveFixInt = 0x00;
            public const byte Fmt_MaxPositiveFixInt = 0x7f;
            public const byte Fmt_MinFixMap = 0x80;
            public const byte Fmt_MaxFixMap = 0x8f;
            public const byte Fmt_MinFixArray = 0x90;
            public const byte Fmt_MaxFixArray = 0x9f;
            public const byte Fmt_MinFixStr = 0xa0;
            public const byte Fmt_MaxFixStr = 0xbf;
            public const byte Fmt_Nil = 0xc0;
            public const byte Fmt_NeverUsed = 0xc1;
            public const byte Fmt_False = 0xc2;
            public const byte Fmt_True = 0xc3;
            public const byte Fmt_Bin8 = 0xc4;
            public const byte Fmt_Bin16 = 0xc5;
            public const byte Fmt_Bin32 = 0xc6;
            public const byte Fmt_Ext8 = 0xc7;
            public const byte Fmt_Ext16 = 0xc8;
            public const byte Fmt_Ext32 = 0xc9;
            public const byte Fmt_Float32 = 0xca;
            public const byte Fmt_Float64 = 0xcb;
            public const byte Fmt_Uint8 = 0xcc;
            public const byte Fmt_Uint16 = 0xcd;
            public const byte Fmt_Uint32 = 0xce;
            public const byte Fmt_Uint64 = 0xcf;
            public const byte Fmt_Int8 = 0xd0;
            public const byte Fmt_Int16 = 0xd1;
            public const byte Fmt_Int32 = 0xd2;
            public const byte Fmt_Int64 = 0xd3;
            public const byte Fmt_FixExt1 = 0xd4;
            public const byte Fmt_FixExt2 = 0xd5;
            public const byte Fmt_FixExt4 = 0xd6;
            public const byte Fmt_FixExt8 = 0xd7;
            public const byte Fmt_FixExt16 = 0xd8;
            public const byte Fmt_Str8 = 0xd9;
            public const byte Fmt_Str16 = 0xda;
            public const byte Fmt_Str32 = 0xdb;
            public const byte Fmt_Array16 = 0xdc;
            public const byte Fmt_Array32 = 0xdd;
            public const byte Fmt_Map16 = 0xde;
            public const byte Fmt_Map32 = 0xdf;
            public const byte Fmt_MinNegativeFixInt = 0xe0;
            public const byte Fmt_MaxNegativeFixInt = 0xff;
#endregion //Output formats

#region Source Types
            public const byte Type_Unknown = 0;
            public const byte Type_Integer = 1;
            public const byte Type_Nil = 2;
            public const byte Type_Boolean = 3;
            public const byte Type_Float = 4;
            public const byte Type_String = 5;
            public const byte Type_Binary = 6;
            public const byte Type_Array = 7;
            public const byte Type_Map = 8;
            public const byte Type_Extension = 9;
#endregion //Source Types

#region Reserved Ext TypeCode
            public const sbyte ExtTypeCode_Timestamp = -1;
#endregion //Reserved Ext TypeCode

            public static byte GetSourceType(byte token)
            {
                return _typeLookupTable[token];
            }
            public static string GetFormatName(byte token)
            {
                return _fmtNameLookupTable[token];
            }
            public static bool IsNegativeFixInt(byte token)
            {
                return Fmt_MinNegativeFixInt <= token && token <= Fmt_MaxNegativeFixInt;
            }
            public static bool IsPositiveFixInt(byte token)
            {
                return Fmt_MinPositiveFixInt <= token && token <= Fmt_MaxPositiveFixInt;
            }
            public static bool IsFixStr(byte token)
            {
                return Fmt_MinFixStr <= token && token <= Fmt_MaxFixStr;
            }
            public static bool IsFixMap(byte token)
            {
                return Fmt_MinFixMap <= token && token <= Fmt_MaxFixMap;
            }
            public static bool IsFixArray(byte token)
            {
                return Fmt_MinFixArray <= token && token <= Fmt_MaxFixArray;
            }
            public static bool IsByte(byte token)
            {
                if (token == Fmt_Uint8) return true;
                if (IsPositiveFixInt(token)) return true;

                return false;
            }
            public static bool IsSByte(byte token)
            {
                if (token == Fmt_Int8) return true;
                if (IsNegativeFixInt(token)) return true;
                return false;
            }
            public static bool IsShort(byte token)
            {
                return (token == Fmt_Int16);
            }
            public static bool IsUShort(byte token)
            {
                return (token == Fmt_Uint16);
            }
            public static bool IsInt(byte token)
            {
                return (token == Fmt_Int32);
            }
            public static bool IsUInt(byte token)
            {
                return (token == Fmt_Uint32);
            }
            public static bool IsLong(byte token)
            {
                return (token == Fmt_Int64);
            }
            public static bool IsULong(byte token)
            {
                return (token == Fmt_Uint64);
            }
            public static bool IsFloat(byte token)
            {
                return (token == Fmt_Float32);
            }
            public static bool IsDouble(byte token)
            {
                return (token == Fmt_Float64);
            }
            public static bool IsString(byte token)
            {
                switch(token)
                {
                    case Fmt_Str8:
                    case Fmt_Str16:
                    case Fmt_Str32:
                        return true;
                    default:
                        return IsFixStr(token);
                }
            }
            public static bool IsBinary(byte token)
            {
                switch(token)
                {
                    case Fmt_Bin8:
                    case Fmt_Bin16:
                    case Fmt_Bin32:
                        return true;
                    default:
                        return false;
                }
            }
            public static bool IsArray(byte token)
            {
                switch(token)
                {
                    case Fmt_Array16:
                    case Fmt_Array32:
                        return true;
                    default:
                        return IsFixArray(token);
                }
            }
            public static bool IsMap(byte token)
            {
                switch(token)
                {
                    case Fmt_Map16:
                    case Fmt_Map32:
                        return true;
                    default:
                        return IsFixMap(token);
                }
            }
            public static bool IsExtension(byte token)
            {
                switch(token)
                {
                    case Fmt_FixExt1:
                    case Fmt_FixExt2:
                    case Fmt_FixExt4:
                    case Fmt_FixExt8:
                    case Fmt_FixExt16:
                    case Fmt_Ext8:
                    case Fmt_Ext16:
                    case Fmt_Ext32:
                        return true;
                    default:
                        return false;
                }
            }
            public static bool IsNil(byte token)
            {
                return (token == Fmt_Nil);
            }

#region Type lookup tables
            private static readonly byte[] _typeLookupTable = new byte[0xff+1];
            private static readonly string[] _fmtNameLookupTable = new string[0xff+1];
#endregion //Type lookup tables

            static Spec()
            {
                for (int i = Fmt_MinPositiveFixInt; i <= Fmt_MaxPositiveFixInt; i++)
                {
                    _typeLookupTable[i] = Type_Integer;
                    _fmtNameLookupTable[i] = "positive fixint";
                }
                for (int i = Fmt_MinFixMap; i <= Fmt_MaxFixMap; i++)
                {
                    _typeLookupTable[i] = Type_Map;
                    _fmtNameLookupTable[i] = "fixmap";
                }
                for (int i = Fmt_MinFixArray; i <= Fmt_MaxFixArray; i++)
                {
                    _typeLookupTable[i] = Type_Array;
                    _fmtNameLookupTable[i] = "fixarray";
                }
                for (int i = Fmt_MinFixStr; i <= Fmt_MaxFixStr; i++)
                {
                    _typeLookupTable[i] = Type_String;
                    _fmtNameLookupTable[i] = "fixstr";
                }
                _typeLookupTable[Fmt_Nil] = Type_Nil; _fmtNameLookupTable[Fmt_Nil] = "nil";
                _typeLookupTable[Fmt_NeverUsed] = Type_Unknown; _fmtNameLookupTable[Fmt_NeverUsed] = "(never used)";
                _typeLookupTable[Fmt_False] = Type_Boolean; _fmtNameLookupTable[Fmt_False] = "false";
                _typeLookupTable[Fmt_True] = Type_Boolean; _fmtNameLookupTable[Fmt_True] = "true";
                _typeLookupTable[Fmt_Bin8] = Type_Binary; _fmtNameLookupTable[Fmt_Bin8] = "bin 8";
                _typeLookupTable[Fmt_Bin16] = Type_Binary; _fmtNameLookupTable[Fmt_Bin16] = "bin 16";
                _typeLookupTable[Fmt_Bin32] = Type_Binary; _fmtNameLookupTable[Fmt_Bin32] = "bin 32";
                _typeLookupTable[Fmt_Ext8] = Type_Extension; _fmtNameLookupTable[Fmt_Ext8] = "ext 8";
                _typeLookupTable[Fmt_Ext16] = Type_Extension; _fmtNameLookupTable[Fmt_Ext16] = "ext 16";
                _typeLookupTable[Fmt_Ext32] = Type_Extension; _fmtNameLookupTable[Fmt_Ext32] = "ext 32";
                _typeLookupTable[Fmt_Float32] = Type_Float; _fmtNameLookupTable[Fmt_Float32] = "float 32";
                _typeLookupTable[Fmt_Float64] = Type_Float; _fmtNameLookupTable[Fmt_Float32] = "float 64";
                _typeLookupTable[Fmt_Uint8] = Type_Integer; _fmtNameLookupTable[Fmt_Uint8] = "uint 8";
                _typeLookupTable[Fmt_Uint16] = Type_Integer; _fmtNameLookupTable[Fmt_Uint16] = "uint 16";
                _typeLookupTable[Fmt_Uint32] = Type_Integer; _fmtNameLookupTable[Fmt_Uint32] = "uint 32";
                _typeLookupTable[Fmt_Uint64] = Type_Integer; _fmtNameLookupTable[Fmt_Uint64] = "uint 64";
                _typeLookupTable[Fmt_Int8] = Type_Integer; _fmtNameLookupTable[Fmt_Int8] = "int 8";
                _typeLookupTable[Fmt_Int16] = Type_Integer; _fmtNameLookupTable[Fmt_Int16] = "int 16";
                _typeLookupTable[Fmt_Int32] = Type_Integer; _fmtNameLookupTable[Fmt_Int32] = "int 32";
                _typeLookupTable[Fmt_Int64] = Type_Integer; _fmtNameLookupTable[Fmt_Int64] = "int 64";
                _typeLookupTable[Fmt_FixExt1] = Type_Extension; _fmtNameLookupTable[Fmt_FixExt1] = "fixext 1";
                _typeLookupTable[Fmt_FixExt2] = Type_Extension; _fmtNameLookupTable[Fmt_FixExt2] = "fixext 2";
                _typeLookupTable[Fmt_FixExt4] = Type_Extension; _fmtNameLookupTable[Fmt_FixExt4] = "fixext 4";
                _typeLookupTable[Fmt_FixExt8] = Type_Extension; _fmtNameLookupTable[Fmt_FixExt8] = "fixext 8";
                _typeLookupTable[Fmt_FixExt16] = Type_Extension; _fmtNameLookupTable[Fmt_FixExt16] = "fixext 16";
                _typeLookupTable[Fmt_Str8] = Type_String; _fmtNameLookupTable[Fmt_Str8] = "str 8";
                _typeLookupTable[Fmt_Str16] = Type_String; _fmtNameLookupTable[Fmt_Str16] = "str 16";
                _typeLookupTable[Fmt_Str32] = Type_String; _fmtNameLookupTable[Fmt_Str32] = "str 32";
                _typeLookupTable[Fmt_Array16] = Type_Array; _fmtNameLookupTable[Fmt_Array16] = "array 16";
                _typeLookupTable[Fmt_Array32] = Type_Array; _fmtNameLookupTable[Fmt_Array32] = "array 32";
                _typeLookupTable[Fmt_Map16] = Type_Map; _fmtNameLookupTable[Fmt_Map16] = "map 16";
                _typeLookupTable[Fmt_Map32] = Type_Map; _fmtNameLookupTable[Fmt_Map32] = "map 32";
                for (int i = Fmt_MinNegativeFixInt; i <= Fmt_MaxNegativeFixInt; i++)
                {
                    _typeLookupTable[i] = Type_Integer;
                    _fmtNameLookupTable[i] = "negative fixint";
                }
            }
        }
    }
}
