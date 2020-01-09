using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackReader2
{
    /// <summary>
    /// .NET4.x/C#7.2/unsafeを使ったMessagePackReader実装
    /// .NET3.5版より遅いがメモリ効率は良い
    /// 　※ReadOnlySpan<byte>の[index]参照はbyte[]の[index]参照より少し遅い
    /// </summary>
    public readonly ref partial struct MsgPackView
    {
        public static MsgPackView Create(byte[] data)
        {
            return Create(new ReadOnlySpan<byte>(data));
        }

        public static MsgPackView Create(ReadOnlyMemory<byte> data)
        {
            return Create(data.Span);
        }

        public static MsgPackView Create(ReadOnlySpan<byte> data)
        {
            return new MsgPackView(data);
        }

        public int GetArrayLength()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadArrayLength(out int v)
            );
            return v;
        }

        public static byte[] KeyToBytes(string key)
        {
            return System.Text.Encoding.UTF8.GetBytes(key);
        }

        public static ReadOnlySpan<byte> KeyToSpan(string key)
        {
            return new ReadOnlySpan<byte>(KeyToBytes(key));
        }

        public MsgPackView this[string key] => this[KeyToBytes(key)];
        public MsgPackView this[byte[] key] => this[new ReadOnlySpan<byte>(key)];

        public MsgPackView this[ReadOnlySpan<byte> key]
        {
            get
            {
                ThrowEndOfStreamExceptionUnless(
                    new SequentialReader(_span).TryReadMapValuePosition(key, out int v)
                );
                return new MsgPackView(_span.Slice(v));
            }
        }

        public MapEnumerable AsMapEnumerable() => new MapEnumerable(_span);

        public MsgPackView this[int index]
        {
            get
            {
                ThrowEndOfStreamExceptionUnless(
                    new SequentialReader(_span).TryReadArrayElementPosition(index, out int v)
                );
                return new MsgPackView(_span.Slice(v));
            }
        }

        public ArrayEnumerable AsArrayEnumerable()
        {
            return new ArrayEnumerable(_span);
        }

        public byte GetByte()
        {
            var reader = new SequentialReader(_span);
            ThrowEndOfStreamExceptionUnless(reader.TryReadByte(out byte v));
            return v;
        }

        public sbyte GetSByte()
        {
            var reader = new SequentialReader(_span);
            ThrowEndOfStreamExceptionUnless(reader.TryReadSByte(out sbyte v));
            return unchecked((sbyte) v);
        }

        public short GetShort()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadShort(out short v)
            );
            return v;
        }

        public ushort GetUShort()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadUShort(out ushort v)
            );
            return v;
        }

        public int GetInt()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadInt(out int v)
            );
            return v;
        }

        public uint GetUInt()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadUInt(out uint v)
            );
            return v;
        }

        public long GetLong()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadLong(out long v)
            );
            return v;
        }

        public ulong GetULong()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadULong(out ulong v)
            );
            return v;
        }

        public float GetFloat()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadFloat(out float v)
            );
            return v;
        }

        public double GetDouble()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadDouble(out double v)
            );
            return v;
        }

        public Nil GetNil()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadNil(out Nil v)
            );
            return v;
        }

        public bool GetBool()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadBool(out bool v)
            );
            return v;
        }

        public string GetString()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadString(out string v)
            );
            return v;
        }

        public byte[] GetBinary()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadBinary(out byte[] v)
            );
            return v;
        }

        public MessagePackExtension GetExtension()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadExtension(out sbyte typeCode, out byte[] v)
            );
            return new MessagePackExtension(typeCode, v);
        }

        public DateTime GetTimestamp()
        {
            ThrowEndOfStreamExceptionUnless(
                new SequentialReader(_span).TryReadTimestamp(out DateTime v)
            );
            return v;
        }
    }

    public class MessagePackReaderException : Exception
    {
        public MessagePackReaderException()
        {
        }

        public MessagePackReaderException(string message) : base(message)
        {
        }
    }

    public readonly struct MessagePackExtension
    {
        public readonly sbyte TypeCode;
        public readonly byte[] Data;

        public MessagePackExtension(sbyte typeCode, byte[] data)
        {
            TypeCode = typeCode;
            Data = data;
        }
    }

    public readonly ref partial struct MsgPackView
    {
        readonly ReadOnlySpan<byte> _span;

        MsgPackView(ReadOnlySpan<byte> data)
        {
            _span = data;
        }

        MessagePackReaderException ThrowEndOfStreamException() => throw new EndOfStreamException();

        void ThrowEndOfStreamExceptionUnless(bool cond)
        {
            if (!cond)
            {
                ThrowEndOfStreamException();
            }
        }

        public ref struct MapEnumerable
        {
            private readonly ReadOnlySpan<byte> _span;

            internal MapEnumerable(ReadOnlySpan<byte> span)
            {
                _span = span;
            }

            public MapEnumerator GetEnumerator() => new MapEnumerator(_span);
        }

        public ref struct MapEnumerator
        {
            private SequentialReader _reader;
            private readonly int _count;
            private int _index;
            private ReadOnlySpan<byte> _currentMapKey;
            private ReadOnlySpan<byte> _currentMapValue;

            internal MapEnumerator(ReadOnlySpan<byte> span)
            {
                _reader = new SequentialReader(span);
                if (!_reader.TryReadMapCount(out _count))
                {
                    throw new MessagePackReaderException();
                }

                _index = 0;
                _currentMapKey = ReadOnlySpan<byte>.Empty;
                _currentMapValue = ReadOnlySpan<byte>.Empty;
            }

            public bool MoveNext()
            {
                if (_index < _count)
                {
                    if (!_reader.TryReadString(out ReadOnlySpan<byte> span))
                    {
                        _index = _count + 1;
                        return false; //key
                    }

                    _currentMapValue = _reader.UnreadSpan;
                    if (!_reader.TrySkipElement())
                    {
                        _index = _count + 1;
                        return false; //value
                    }

                    return true;
                }

                _index = _count + 1;
                return false;
            }

            public MessagePackMapElement Current()
            {
                return new MessagePackMapElement(_currentMapKey, new MsgPackView(_currentMapValue));
            }
        }

        public ref struct MessagePackMapElement
        {
            public readonly ReadOnlySpan<byte> Key;
            public string KeyAsString => StringUtil.GetString(Key);
            public readonly MsgPackView Value;

            internal MessagePackMapElement(ReadOnlySpan<byte> key, in MsgPackView value)
            {
                Key = key;
                Value = value;
            }
        }

        public ref struct ArrayEnumerable
        {
            private readonly ReadOnlySpan<byte> _span;

            internal ArrayEnumerable(ReadOnlySpan<byte> span)
            {
                _span = span;
            }

            public ArrayEnumerator GetEnumerator() => new ArrayEnumerator(_span);
        }

        public ref struct ArrayEnumerator
        {
            private SequentialReader _reader;
            private readonly int _length;
            private int _index;
            private ReadOnlySpan<byte> _current;

            internal ArrayEnumerator(ReadOnlySpan<byte> span)
            {
                _reader = new SequentialReader(span);
                if (!_reader.TryReadArrayLength(out _length))
                {
                    throw new MessagePackReaderException();
                }

                _index = 0;
                _current = ReadOnlySpan<byte>.Empty;
            }

            public bool MoveNext()
            {
                if (_index < _length)
                {
                    _current = _reader.UnreadSpan;
                    if (!_reader.TrySkipElement())
                    {
                        _index = _length + 1;
                        return false;
                    }

                    _index++;
                    return true;
                }

                _index = _length + 1;
                return false;
            }

            public MsgPackView Current
            {
                get => new MsgPackView(_current);
            }
        }

        ref struct SequentialReader
        {
            private readonly ReadOnlySpan<byte> _source;
            private int _position;

            public SequentialReader(ReadOnlySpan<byte> source)
            {
                _source = source;
                _position = 0;
            }

            public ReadOnlySpan<byte> UnreadSpan => _source.Slice(_position);

            public int Position => _position;

            /// <summary>
            /// Seek位置のTokenを取得
            /// </summary>
            public byte PeekToken => _source[_position];

            /// <summary>
            /// Seek位置のTokenを取得し位置を進める
            /// </summary>
            public bool TryReadToken(out byte token)
            {
                if (_source.Length <= _position)
                {
                    token = default;
                    return false;
                }

                token = _source[_position];
                _position++;
                return true;
            }

            public bool TryReadNil(out Nil v)
            {
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                if (!Spec.IsNil(token)) return false;
                v = Nil.Default;
                return true;
            }

            public bool TryReadMapCount(out int v)
            {
                v = default;
                if (!TryReadToken(out byte token)) return false;
                switch (token)
                {
                    case Spec.Fmt_Map16:
                        if (!TryRead(out ushort ushortResult)) return false;
                        v = ushortResult;
                        break;
                    case Spec.Fmt_Map32:
                        if (!TryRead(out uint uintResult)) return false;
                        v = (int) uintResult;
                        break;
                    default:
                        if (Spec.Fmt_MinFixMap <= token && token <= Spec.Fmt_MaxFixMap)
                        {
                            v = (int) (token & 0xF);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            bool TryReadMapElementCount(out int v)
            {
                v = default;
                if (!TryReadToken(out byte token)) return false;

                switch (token)
                {
                    case Spec.Fmt_Map16:
                        if (!TryRead(out ushort ushortResult)) return false;
                        v = ushortResult;
                        break;
                    case Spec.Fmt_Map32:
                        if (!TryRead(out uint uintResult)) return false;
                        v = (int) uintResult;
                        break;
                    default:
                        if (Spec.Fmt_MinFixMap <= token && token <= Spec.Fmt_MaxFixMap)
                        {
                            v = (int) (token & 0xF);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            public bool TryReadArrayLength(out int v)
            {
                v = default;
                if (!TryReadToken(out byte token)) return false;

                switch (token)
                {
                    case Spec.Fmt_Array16:
                        if (!TryRead(out ushort ushortResult)) return false;
                        v = ushortResult;
                        break;
                    case Spec.Fmt_Array32:
                        if (!TryRead(out uint uintResult)) return false;
                        v = (int) uintResult;
                        break;
                    default:
                        if (!Spec.IsFixArray(token)) return false;
                        v = (byte) (token & 0xF);
                        break;
                }

                return true;
            }

            public bool TryReadMapValuePosition(ReadOnlySpan<byte> key, out int v)
            {
                v = default;
                if (!TryReadMapElementCount(out int mapCount)) return false;

                for (int i = 0; i < mapCount; i++)
                {
                    if (!TryReadAndCompareMapKey(key, out bool compareResult)) return false;
                    if (compareResult)
                    {
                        v = _position;
                        return true;
                    }
                    else
                    {
                        if (!TrySkipElement()) return false; //value
                    }
                }

                return false;
            }

            unsafe bool TryReadAndCompareMapKey(ReadOnlySpan<byte> key, out bool v)
            {
                v = default;
                var token = PeekToken;
                if (token == Spec.Fmt_Nil) return false;

                if (!TryReadStringByteLength(out int stringByteLength)) return false;
                if (key.Length != stringByteLength)
                {
                    v = false;
                    _position += stringByteLength;
                    return true;
                }

                v = CompareSpan(_source.Slice(_position, stringByteLength), key);

                v = true;
                _position += stringByteLength;
                return true;
            }

            unsafe bool CompareSpan(ReadOnlySpan<byte> lh, ReadOnlySpan<byte> rh)
            {
                int length = lh.Length;
                int lengthLong = length / sizeof(ulong) * sizeof(ulong);
                fixed (byte* ptrL = &MemoryMarshal.GetReference(lh))
                {
                    fixed (byte* ptrR = &MemoryMarshal.GetReference(rh))
                    {
                        int i = 0;
                        for (; i < lengthLong; i += sizeof(ulong))
                        {
                            if (*(ulong*) (ptrL + i) != *(ulong*) (ptrR + i)) return false;
                        }

                        for (; i < length; i++)
                        {
                            if (*(ptrL + i) != *(ptrR + i)) return false;
                        }
                    }
                }

                return true;
            }

            public bool TryReadArrayElementPosition(int index, out int v)
            {
                v = default;
                if (!TryReadArrayLength(out int length)) return false;
                if (index > length) return false;

                for (int i = 0; i < index; i++)
                {
                    if (!TrySkipElement()) return false;
                }

                v = _position;
                return true;
            }

            public bool TrySkipElement()
            {
                var token = PeekToken;
                var sourceType = Spec.GetSourceType(token);

                switch (sourceType)
                {
                    case Spec.Type_Integer:
                        _position += sizeof(byte);
                        if (Spec.IsNegativeFixInt(token) || Spec.IsPositiveFixInt(token))
                        {
                            //nothing to do
                        }
                        else if (token == Spec.Fmt_Int8 || token == Spec.Fmt_Uint8)
                        {
                            _position += sizeof(byte);
                        }
                        else if (token == Spec.Fmt_Int16 || token == Spec.Fmt_Uint16)
                        {
                            _position += sizeof(short);
                        }
                        else if (token == Spec.Fmt_Int32 || token == Spec.Fmt_Uint32)
                        {
                            _position += sizeof(int);
                        }
                        else if (token == Spec.Fmt_Int64 || token == Spec.Fmt_Uint64)
                        {
                            _position += sizeof(long);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    case Spec.Type_Boolean:
                        _position += sizeof(byte);
                        break;
                    case Spec.Type_Float:
                        _position += sizeof(byte);
                        if (token == Spec.Fmt_Float32)
                        {
                            _position += sizeof(float);
                        }
                        else if (token == Spec.Fmt_Float64)
                        {
                            _position += sizeof(double);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    case Spec.Type_String:
                        if (!TryReadStringByteLength(out int stringByteLength)) return false;
                        _position += stringByteLength;
                        break;
                    case Spec.Type_Binary:
                        if (!TryReadBinaryByteLength(out int binaryByteLength)) return false;
                        _position += binaryByteLength;
                        break;
                    case Spec.Type_Extension:
                        if (!TryReadExtensionHeader(out ExtensionHeader header)) return false;
                        _position += header.Length;
                        break;
                    case Spec.Type_Array:
                        if (!TryReadArrayLength(out int arrayLength)) return false;
                        for (int i = 0; i < arrayLength; i++)
                        {
                            if (!TrySkipElement()) return false;
                        }

                        break;
                    case Spec.Type_Map:
                        if (!TryReadMapCount(out int mapCount)) return true;
                        for (int i = 0; i < mapCount; i++)
                        {
                            if (!TrySkipElement()) return false; //key
                            if (!TrySkipElement()) return false; //value
                        }

                        break;
                    case Spec.Type_Nil:
                        _position += sizeof(byte);
                        break;
                    default:
                        return false;
                }

                return true;
            }

            public bool TryReadByte(out byte v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out v))
                        {
                            return false;
                        }

                        ;
                        break;
                    default:
                        if (!Spec.IsPositiveFixInt(token))
                        {
                            return false;
                        }

                        v = token;
                        break;
                }

                return true;
            }

            public bool TryReadSByte(out sbyte v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out byte tmp))
                        {
                            return false;
                        }

                        ;
                        v = unchecked((sbyte) tmp);
                        break;
                    case Spec.Fmt_Int8:
                        if (!TryRead(out v))
                        {
                            return false;
                        }

                        break;
                    default:
                        if (Spec.IsPositiveFixInt(token) || Spec.IsNegativeFixInt(token))
                        {
                            v = unchecked((sbyte) token);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            public bool TryReadShort(out short v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out byte byteResult))
                        {
                            return false;
                        }

                        v = byteResult;
                        break;
                    case Spec.Fmt_Int8:
                        if (!TryRead(out sbyte sbyteResult))
                        {
                            return false;
                        }

                        v = sbyteResult;
                        break;
                    case Spec.Fmt_Int16:
                        if (!TryRead(out v))
                        {
                            return false;
                        }

                        break;
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            v = token;
                        }
                        else if (Spec.IsNegativeFixInt(token))
                        {
                            v = unchecked((sbyte) token);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            public bool TryReadUShort(out ushort v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out byte byteResult))
                        {
                            return false;
                        }

                        v = byteResult;
                        break;
                    case Spec.Fmt_Uint16:
                        if (!TryRead(out v))
                        {
                            return false;
                        }

                        break;
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            v = token;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            public bool TryReadInt(out int v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out byte byteResult))
                        {
                            return false;
                        }

                        v = byteResult;
                        break;
                    case Spec.Fmt_Int8:
                        if (!TryRead(out sbyte sbyteResult))
                        {
                            return false;
                        }

                        v = sbyteResult;
                        break;
                    case Spec.Fmt_Uint16:
                        if (!TryRead(out ushort ushortResult))
                        {
                            return false;
                        }

                        v = ushortResult;
                        break;
                    case Spec.Fmt_Int16:
                        if (!TryRead(out short shortResult))
                        {
                            return false;
                        }

                        v = shortResult;
                        break;
                    case Spec.Fmt_Int32:
                        if (!TryRead(out v))
                        {
                            return false;
                        }

                        break;
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            v = token;
                        }
                        else if (Spec.IsNegativeFixInt(token))
                        {
                            v = unchecked((sbyte) token);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            public bool TryReadUInt(out uint v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out byte byteResult)) return false;
                        v = byteResult;
                        break;
                    case Spec.Fmt_Uint16:
                        if (!TryRead(out ushort ushortResult)) return false;
                        v = ushortResult;
                        break;
                    case Spec.Fmt_Uint32:
                        if (!TryRead(out v)) return false;
                        break;
                    default:
                        if (!Spec.IsPositiveFixInt(token)) return false;
                        v = token;
                        break;
                }

                return true;
            }

            public bool TryReadLong(out long v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out byte byteResult)) return false;
                        v = byteResult;
                        break;
                    case Spec.Fmt_Int8:
                        if (!TryRead(out sbyte sbyteResult)) return false;
                        v = sbyteResult;
                        break;
                    case Spec.Fmt_Uint16:
                        if (!TryRead(out ushort ushortResult)) return false;
                        v = ushortResult;
                        break;
                    case Spec.Fmt_Int16:
                        if (!TryRead(out short shortResult)) return false;
                        v = shortResult;
                        break;
                    case Spec.Fmt_Uint32:
                        if (!TryRead(out uint uintResult)) return false;
                        v = uintResult;
                        break;
                    case Spec.Fmt_Int32:
                        if (!TryRead(out int intResult)) return false;
                        v = intResult;
                        break;
                    case Spec.Fmt_Int64:
                        if (!TryRead(out v)) return false;
                        break;
                    default:
                        if (Spec.IsPositiveFixInt(token))
                        {
                            v = token;
                        }
                        else if (Spec.IsNegativeFixInt(token))
                        {
                            v = unchecked((sbyte) token);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            public bool TryReadULong(out ulong v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Uint8:
                        if (!TryRead(out byte byteResult)) return false;
                        v = byteResult;
                        break;
                    case Spec.Fmt_Uint16:
                        if (!TryRead(out ushort ushortResult)) return false;
                        v = ushortResult;
                        break;
                    case Spec.Fmt_Uint32:
                        if (!TryRead(out uint uintResult)) return false;
                        v = uintResult;
                        break;
                    case Spec.Fmt_Uint64:
                        if (!TryRead(out v)) return false;
                        break;
                    default:
                        if (!Spec.IsPositiveFixInt(token)) return false;
                        v = token;
                        break;
                }

                return true;
            }

            public bool TryReadFloat(out float v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                if (!Spec.IsFloat(token)) return false;
                if (!TryRead(out v)) return false;

                return true;
            }

            public bool TryReadDouble(out double v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_Float32:
                        if (!TryRead(out float floatResult)) return false;
                        v = floatResult;
                        break;
                    case Spec.Fmt_Float64:
                        if (!TryRead(out v)) return false;
                        break;
                    default:
                        return false;
                }

                return true;
            }

            public bool TryReadBool(out bool v)
            {
                v = default;
                if (!TryReadToken(out byte token))
                {
                    return false;
                }

                switch (token)
                {
                    case Spec.Fmt_False:
                        v = false;
                        break;
                    case Spec.Fmt_True:
                        v = true;
                        break;
                    default:
                        return false;
                }

                return true;
            }

            public bool TryReadString(out ReadOnlySpan<byte> v)
            {
                v = ReadOnlySpan<byte>.Empty;
                var token = PeekToken;
                if (token == Spec.Fmt_Nil)
                {
                    //空文字列の場合nil
                    return true;
                }

                if (!TryReadStringByteLength(out int stringByteLength)) return false;
                v = _source.Slice(_position, stringByteLength);
                return true;
            }

            public bool TryReadString(out string v)
            {
                v = default;
                if (!TryReadString(out ReadOnlySpan<byte> span)) return false;
                v = StringUtil.GetString(span);
                return true;
            }

            bool TryReadStringByteLength(out int v)
            {
                v = default;
                if (!TryReadToken(out byte token)) return false;
                switch (token)
                {
                    case Spec.Fmt_Str8:
                        if (!TryRead(out byte byteResult)) return false;
                        v = byteResult;
                        break;
                    case Spec.Fmt_Str16:
                        if (!TryRead(out short shortResult)) return false;
                        v = shortResult;
                        break;
                    case Spec.Fmt_Str32:
                        if (!TryRead(out int intResult)) return false;
                        v = intResult;
                        break;
                    default:
                        if (Spec.Fmt_MinFixStr <= token && token <= Spec.Fmt_MaxFixStr)
                        {
                            v = (token & 0x1F);
                        }
                        else
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }

            public bool TryReadBinary(out byte[] v)
            {
                v = default;
                if (!TryReadBinaryByteLength(out int byteLength)) return false;
                v = _source.Slice(_position, byteLength).ToArray();
                return true;
            }

            bool TryReadBinaryByteLength(out int v)
            {
                v = default;
                if (!TryReadToken(out byte token)) return false;

                switch (token)
                {
                    case Spec.Fmt_Bin8:
                        if (!TryRead(out byte byteResult)) return false;
                        v = byteResult;
                        break;
                    case Spec.Fmt_Bin16:
                        if (!TryRead(out ushort ushortResult)) return false;
                        v = ushortResult;
                        break;
                    case Spec.Fmt_Bin32:
                        if (!TryRead(out uint uintResult)) return false;
                        v = (int) uintResult;
                        break;
                    default:
                        return false;
                }

                return true;
            }

            public bool TryReadExtension(out sbyte typeCode, out byte[] data)
            {
                typeCode = default;
                data = default;
                if (!TryReadExtensionHeader(out ExtensionHeader header)) return false;

                typeCode = header.TypeCode;
                data = _source.Slice(_position, header.Length).ToArray();
                return true;
            }

            public bool TryReadTimestamp(out DateTime v)
            {
                v = default;
                if (TryReadExtensionHeader(out ExtensionHeader header)) return false;
                if (header.TypeCode != Spec.ExtTypeCode_Timestamp) return false;

                switch (header.Length)
                {
                    case 4:
                    {
                        if (!TryRead(out uint uintResult)) return false;
                        v = DateTimeConverter.GetDateTime(uintResult);
                    }
                        break;
                    case 8:
                    {
                        if (!TryRead(out ulong ulongResult)) return false;
                        uint nanoseconds = (uint) (ulongResult >> 34);
                        uint seconds = (uint) (ulongResult & 0x00000003ffffffffL);
                        v = DateTimeConverter.GetDateTime(seconds, nanoseconds);
                    }
                        break;
                    case 12:
                    {
                        if (!TryRead(out uint uintResult)) return false;
                        if (!TryRead(out long longResult)) return false;

                        v = DateTimeConverter.GetDateTime(longResult, uintResult);
                    }
                        break;
                    default:
                        return false;
                }

                return true;
            }

            bool TryReadExtensionHeader(out ExtensionHeader v)
            {
                v = default;
                if (!TryReadToken(out byte token)) return false;

                int length = default;
                switch (token)
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
                        if (!TryRead(out byte byteResult)) return false;
                        length = byteResult;
                        break;
                    case Spec.Fmt_Ext16:
                        if (!TryRead(out short shortResult)) return false;
                        length = shortResult;
                        break;
                    case Spec.Fmt_Ext32:
                        if (!TryRead(out length)) return false;
                        break;
                    default:
                        return false;
                }

                if (!TryRead(out sbyte typeCode)) return false;
                v = new ExtensionHeader(typeCode, length);
                return true;
            }

            bool TryRead<T>(out T value) where T : struct
            {
                value = default(T);
                var typeSize = TypeSizeProvider.Instance.Get<T>();
                if ((_source.Length - _position) < typeSize)
                {
                    return false;
                }

                value = BytesConverterResolver.Instance.GetConverter<T>().To(_source.Slice(_position));
                _position += typeSize;
                return true;
            }

            readonly struct ExtensionHeader
            {
                public readonly sbyte TypeCode;
                public readonly int Length;

                public ExtensionHeader(sbyte typeCode, int length)
                {
                    TypeCode = typeCode;
                    Length = length;
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

                TypeSizeProvider()
                {
                }

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
                    public static int ToSize(Type type)
                    {
                        return TypeSizeMap[type];
                    }

                    static readonly Dictionary<Type, int> TypeSizeMap = new Dictionary<Type, int>()
                    {
                        {typeof(byte), sizeof(byte)},
                        {typeof(sbyte), sizeof(sbyte)},
                        {typeof(short), sizeof(short)},
                        {typeof(ushort), sizeof(ushort)},
                        {typeof(int), sizeof(int)},
                        {typeof(uint), sizeof(uint)},
                        {typeof(long), sizeof(long)},
                        {typeof(ulong), sizeof(ulong)},
                        {typeof(float), sizeof(float)},
                        {typeof(double), sizeof(double)},
                    };
                }
            } // end of class TypeSizeProvider

            /// <summary>
            /// 型引数に応じたbyte列->型変換処理
            /// </summary>
            interface IBytesConverter<T>
            {
                T To(ReadOnlySpan<byte> span);
            }

            class BytesConverterResolver
            {
                public static readonly BytesConverterResolver Instance = new BytesConverterResolver();

                public IBytesConverter<T> GetConverter<T>()
                {
                    return Cache<T>.converter;
                }

                BytesConverterResolver()
                {
                }

                static class Cache<T>
                {
                    public static readonly IBytesConverter<T> converter;

                    static Cache()
                    {
                        converter =
                            (IBytesConverter<T>) BytesConverterResolverCacheHelper.ToConverter(typeof(T));
                    }
                }

                static class BytesConverterResolverCacheHelper
                {
                    public static object ToConverter(Type type)
                    {
                        return ConverterMap[type];
                    }

                    static readonly Dictionary<Type, object> ConverterMap = new Dictionary<Type, object>()
                    {
                        {typeof(byte), new ByteConverter()},
                        {typeof(sbyte), new SByteConverter()},
                        {typeof(short), new ShortConverter()},
                        {typeof(ushort), new UShortConverter()},
                        {typeof(int), new IntConverter()},
                        {typeof(uint), new UIntConverter()},
                        {typeof(long), new LongConverter()},
                        {typeof(ulong), new ULongConverter()},
                        {typeof(float), new FloatConverter()},
                        {typeof(double), new DoubleConverter()},
                    };
                }

                class ByteConverter : IBytesConverter<byte>
                {
                    public byte To(ReadOnlySpan<byte> span)
                    {
                        return span[0];
                    }
                }

                class SByteConverter : IBytesConverter<sbyte>
                {
                    public sbyte To(ReadOnlySpan<byte> span)
                    {
                        return unchecked((sbyte) span[0]);
                    }
                }

                class ShortConverter : IBytesConverter<short>
                {
                    public short To(ReadOnlySpan<byte> span)
                    {
                        var v = MemoryMarshal.Read<short>(span);
                        return (!BitConverter.IsLittleEndian)
                            ? v
                            : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                    }
                }

                class UShortConverter : IBytesConverter<ushort>
                {
                    public ushort To(ReadOnlySpan<byte> span)
                    {
                        var v = MemoryMarshal.Read<ushort>(span);
                        return (!BitConverter.IsLittleEndian)
                            ? v
                            : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                    }
                }

                class IntConverter : IBytesConverter<int>
                {
                    public int To(ReadOnlySpan<byte> span)
                    {
                        var v = MemoryMarshal.Read<int>(span);
                        return (!BitConverter.IsLittleEndian)
                            ? v
                            : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                    }
                }

                class UIntConverter : IBytesConverter<uint>
                {
                    public uint To(ReadOnlySpan<byte> span)
                    {
                        var v = MemoryMarshal.Read<uint>(span);
                        return (!BitConverter.IsLittleEndian)
                            ? v
                            : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                    }
                }

                class LongConverter : IBytesConverter<long>
                {
                    public long To(ReadOnlySpan<byte> span)
                    {
                        var v = MemoryMarshal.Read<long>(span);
                        return (!BitConverter.IsLittleEndian)
                            ? v
                            : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                    }
                }

                class ULongConverter : IBytesConverter<ulong>
                {
                    public ulong To(ReadOnlySpan<byte> span)
                    {
                        var v = MemoryMarshal.Read<ulong>(span);
                        return (!BitConverter.IsLittleEndian)
                            ? v
                            : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                    }
                }

                class FloatConverter : IBytesConverter<float>
                {
                    public float To(ReadOnlySpan<byte> span)
                    {
                        if (!BitConverter.IsLittleEndian)
                        {
                            return MemoryMarshal.Read<float>(span);
                        }
                        else
                        {
#if false
                            Span<byte> tmp = stackalloc byte[sizeof(float)];
                            tmp[0] = span[3];
                            tmp[1] = span[2];
                            tmp[2] = span[1];
                            tmp[3] = span[0];
                            return MemoryMarshal.Read<float>(tmp);
#else
                            uint tmp = MemoryMarshal.Read<uint>(span);
                            return To(tmp);
#endif
                        }
                    }

                    unsafe float To(uint v)
                    {
                        var tmp = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                        return *(float*) &tmp;
                    }
                }

                class DoubleConverter : IBytesConverter<double>
                {
                    public double To(ReadOnlySpan<byte> span)
                    {
                        if (!BitConverter.IsLittleEndian)
                        {
                            return MemoryMarshal.Read<double>(span);
                        }
                        else
                        {
#if false
                            Span<byte> tmp = stackalloc byte[sizeof(double)];
                            tmp[0] = span[7];
                            tmp[1] = span[6];
                            tmp[2] = span[5];
                            tmp[3] = span[4];
                            tmp[4] = span[3];
                            tmp[5] = span[2];
                            tmp[6] = span[1];
                            tmp[7] = span[0];
                            return MemoryMarshal.Read<double>(tmp);
#else
                            var tmp = MemoryMarshal.Read<ulong>(span);
                            return To(tmp);
#endif
                        }
                    }

                    unsafe double To(ulong v)
                    {
                        var tmp = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
                        return *(double*) &tmp;
                    }
                }
            } // end of class BytesConverterResolver
        } // end of SequentialReader

        public struct Nil : IEquatable<Nil>
        {
            public static readonly Nil Default = default;

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
        } // end of struct Nil

        static class StringUtil
        {
            public static unsafe string GetString(ReadOnlySpan<byte> span)
            {
                fixed (byte* ptr = &MemoryMarshal.GetReference(span))
                {
                    return System.Text.Encoding.UTF8.GetString(ptr, span.Length);
                }
            }
        }

        /// <summary>
        /// MessagePack仕様
        /// <seealso cref="https://github.com/msgpack/msgpack/blob/master/spec.md"/>
        /// </summary>
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
                return TypeLookupTable[token];
            }

            public static string GetFormatName(byte token)
            {
                return FmtNameLookupTable[token];
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
                switch (token)
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
                switch (token)
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
                switch (token)
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
                switch (token)
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
                switch (token)
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

            private static readonly byte[] TypeLookupTable = new byte[0xff + 1];
            private static readonly string[] FmtNameLookupTable = new string[0xff + 1];

            #endregion //Type lookup tables

            static Spec()
            {
                for (int i = Fmt_MinPositiveFixInt; i <= Fmt_MaxPositiveFixInt; i++)
                {
                    TypeLookupTable[i] = Type_Integer;
                    FmtNameLookupTable[i] = "positive fixint";
                }

                for (int i = Fmt_MinFixMap; i <= Fmt_MaxFixMap; i++)
                {
                    TypeLookupTable[i] = Type_Map;
                    FmtNameLookupTable[i] = "fixmap";
                }

                for (int i = Fmt_MinFixArray; i <= Fmt_MaxFixArray; i++)
                {
                    TypeLookupTable[i] = Type_Array;
                    FmtNameLookupTable[i] = "fixarray";
                }

                for (int i = Fmt_MinFixStr; i <= Fmt_MaxFixStr; i++)
                {
                    TypeLookupTable[i] = Type_String;
                    FmtNameLookupTable[i] = "fixstr";
                }

                TypeLookupTable[Fmt_Nil] = Type_Nil;
                FmtNameLookupTable[Fmt_Nil] = "nil";
                TypeLookupTable[Fmt_NeverUsed] = Type_Unknown;
                FmtNameLookupTable[Fmt_NeverUsed] = "(never used)";
                TypeLookupTable[Fmt_False] = Type_Boolean;
                FmtNameLookupTable[Fmt_False] = "false";
                TypeLookupTable[Fmt_True] = Type_Boolean;
                FmtNameLookupTable[Fmt_True] = "true";
                TypeLookupTable[Fmt_Bin8] = Type_Binary;
                FmtNameLookupTable[Fmt_Bin8] = "bin 8";
                TypeLookupTable[Fmt_Bin16] = Type_Binary;
                FmtNameLookupTable[Fmt_Bin16] = "bin 16";
                TypeLookupTable[Fmt_Bin32] = Type_Binary;
                FmtNameLookupTable[Fmt_Bin32] = "bin 32";
                TypeLookupTable[Fmt_Ext8] = Type_Extension;
                FmtNameLookupTable[Fmt_Ext8] = "ext 8";
                TypeLookupTable[Fmt_Ext16] = Type_Extension;
                FmtNameLookupTable[Fmt_Ext16] = "ext 16";
                TypeLookupTable[Fmt_Ext32] = Type_Extension;
                FmtNameLookupTable[Fmt_Ext32] = "ext 32";
                TypeLookupTable[Fmt_Float32] = Type_Float;
                FmtNameLookupTable[Fmt_Float32] = "float 32";
                TypeLookupTable[Fmt_Float64] = Type_Float;
                FmtNameLookupTable[Fmt_Float32] = "float 64";
                TypeLookupTable[Fmt_Uint8] = Type_Integer;
                FmtNameLookupTable[Fmt_Uint8] = "uint 8";
                TypeLookupTable[Fmt_Uint16] = Type_Integer;
                FmtNameLookupTable[Fmt_Uint16] = "uint 16";
                TypeLookupTable[Fmt_Uint32] = Type_Integer;
                FmtNameLookupTable[Fmt_Uint32] = "uint 32";
                TypeLookupTable[Fmt_Uint64] = Type_Integer;
                FmtNameLookupTable[Fmt_Uint64] = "uint 64";
                TypeLookupTable[Fmt_Int8] = Type_Integer;
                FmtNameLookupTable[Fmt_Int8] = "int 8";
                TypeLookupTable[Fmt_Int16] = Type_Integer;
                FmtNameLookupTable[Fmt_Int16] = "int 16";
                TypeLookupTable[Fmt_Int32] = Type_Integer;
                FmtNameLookupTable[Fmt_Int32] = "int 32";
                TypeLookupTable[Fmt_Int64] = Type_Integer;
                FmtNameLookupTable[Fmt_Int64] = "int 64";
                TypeLookupTable[Fmt_FixExt1] = Type_Extension;
                FmtNameLookupTable[Fmt_FixExt1] = "fixext 1";
                TypeLookupTable[Fmt_FixExt2] = Type_Extension;
                FmtNameLookupTable[Fmt_FixExt2] = "fixext 2";
                TypeLookupTable[Fmt_FixExt4] = Type_Extension;
                FmtNameLookupTable[Fmt_FixExt4] = "fixext 4";
                TypeLookupTable[Fmt_FixExt8] = Type_Extension;
                FmtNameLookupTable[Fmt_FixExt8] = "fixext 8";
                TypeLookupTable[Fmt_FixExt16] = Type_Extension;
                FmtNameLookupTable[Fmt_FixExt16] = "fixext 16";
                TypeLookupTable[Fmt_Str8] = Type_String;
                FmtNameLookupTable[Fmt_Str8] = "str 8";
                TypeLookupTable[Fmt_Str16] = Type_String;
                FmtNameLookupTable[Fmt_Str16] = "str 16";
                TypeLookupTable[Fmt_Str32] = Type_String;
                FmtNameLookupTable[Fmt_Str32] = "str 32";
                TypeLookupTable[Fmt_Array16] = Type_Array;
                FmtNameLookupTable[Fmt_Array16] = "array 16";
                TypeLookupTable[Fmt_Array32] = Type_Array;
                FmtNameLookupTable[Fmt_Array32] = "array 32";
                TypeLookupTable[Fmt_Map16] = Type_Map;
                FmtNameLookupTable[Fmt_Map16] = "map 16";
                TypeLookupTable[Fmt_Map32] = Type_Map;
                FmtNameLookupTable[Fmt_Map32] = "map 32";
                for (int i = Fmt_MinNegativeFixInt; i <= Fmt_MaxNegativeFixInt; i++)
                {
                    TypeLookupTable[i] = Type_Integer;
                    FmtNameLookupTable[i] = "negative fixint";
                }
            }
        } //end of class Spec
    }
}