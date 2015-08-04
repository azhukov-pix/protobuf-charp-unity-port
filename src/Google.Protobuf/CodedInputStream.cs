#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Google.Protobuf
{
    /// <summary>
    /// Readings and decodes protocol message fields.
    /// </summary>
    /// <remarks>
    /// This class contains two kinds of methods:  methods that read specific
    /// protocol message constructs and field types (e.g. ReadTag and
    /// ReadInt32) and methods that read low-level values (e.g.
    /// ReadRawVarint32 and ReadRawBytes).  If you are reading encoded protocol
    /// messages, you should use the former methods, but if you are reading some
    /// other format of your own design, use the latter. The names of the former
    /// methods are taken from the protocol buffer type names, not .NET types.
    /// (Hence ReadFloat instead of ReadSingle, and ReadBool instead of ReadBoolean.)
    /// 
    /// TODO(jonskeet): Consider whether recursion and size limits shouldn't be readonly,
    /// set at construction time.
    /// </remarks>
    public sealed class CodedInputStream
    {
        private readonly byte[] buffer;
        private int bufferSize;
        private int bufferSizeAfterLimit = 0;
        private int bufferPos = 0;
        private readonly Stream input;
        private uint lastTag = 0;

        private uint nextTag = 0;
        private bool hasNextTag = false;

        internal const int DefaultRecursionLimit = 64;
        internal const int DefaultSizeLimit = 64 << 20; // 64MB
        internal const int BufferSize = 4096;

        /// <summary>
        /// The total number of bytes read before the current buffer. The
        /// total bytes read up to the current position can be computed as
        /// totalBytesRetired + bufferPos.
        /// </summary>
        private int totalBytesRetired = 0;

        /// <summary>
        /// The absolute position of the end of the current message.
        /// </summary> 
        private int currentLimit = int.MaxValue;

        /// <summary>
        /// <see cref="SetRecursionLimit"/>
        /// </summary>
        private int recursionDepth = 0;

        private int recursionLimit = DefaultRecursionLimit;

        /// <summary>
        /// <see cref="SetSizeLimit"/>
        /// </summary>
        private int sizeLimit = DefaultSizeLimit;

        #region Construction
        /// <summary>
        /// Creates a new CodedInputStream reading data from the given
        /// byte array.
        /// </summary>
        public CodedInputStream(byte[] buf) : this(buf, 0, buf.Length)
        {
        }

        /// <summary>
        /// Creates a new CodedInputStream that reads from the given
        /// byte array slice.
        /// </summary>
        public CodedInputStream(byte[] buffer, int offset, int length)
        {
            this.buffer = buffer;
            this.bufferPos = offset;
            this.bufferSize = offset + length;
            this.input = null;
        }

        /// <summary>
        /// Creates a new CodedInputStream reading data from the given stream.
        /// </summary>
        public CodedInputStream(Stream input)
        {
            this.buffer = new byte[BufferSize];
            this.bufferSize = 0;
            this.input = input;
        }

        /// <summary>
        /// Creates a new CodedInputStream reading data from the given
        /// stream, with a pre-allocated buffer.
        /// </summary>
        internal CodedInputStream(Stream input, byte[] buffer)
        {
            this.buffer = buffer;
            this.bufferSize = 0;
            this.input = input;
        }
        #endregion

        /// <summary>
        /// Returns the current position in the input stream, or the position in the input buffer
        /// </summary>
        public long Position 
        {
            get
            {
                if (input != null)
                {
                    return input.Position - ((bufferSize + bufferSizeAfterLimit) - bufferPos);
                }
                return bufferPos;
            }
        }

        /// <summary>
        /// Returns the last tag read, or 0 if no tags have been read or we've read beyond
        /// the end of the stream.
        /// </summary>
        internal uint LastTag { get { return lastTag; } }

        #region Limits for recursion and length
        /// <summary>
        /// Set the maximum message recursion depth.
        /// </summary>
        /// <remarks>
        /// In order to prevent malicious
        /// messages from causing stack overflows, CodedInputStream limits
        /// how deeply messages may be nested.  The default limit is 64.
        /// </remarks>
        public int SetRecursionLimit(int limit)
        {
            if (limit < 0)
            {
                throw new ArgumentOutOfRangeException("Recursion limit cannot be negative: " + limit);
            }
            int oldLimit = recursionLimit;
            recursionLimit = limit;
            return oldLimit;
        }

        /// <summary>
        /// Set the maximum message size.
        /// </summary>
        /// <remarks>
        /// In order to prevent malicious messages from exhausting memory or
        /// causing integer overflows, CodedInputStream limits how large a message may be.
        /// The default limit is 64MB.  You should set this limit as small
        /// as you can without harming your app's functionality.  Note that
        /// size limits only apply when reading from an InputStream, not
        /// when constructed around a raw byte array (nor with ByteString.NewCodedInput).
        /// If you want to read several messages from a single CodedInputStream, you
        /// can call ResetSizeCounter() after each message to avoid hitting the
        /// size limit.
        /// </remarks>
        public int SetSizeLimit(int limit)
        {
            if (limit < 0)
            {
                throw new ArgumentOutOfRangeException("Size limit cannot be negative: " + limit);
            }
            int oldLimit = sizeLimit;
            sizeLimit = limit;
            return oldLimit;
        }

        /// <summary>
        /// Resets the current size counter to zero (see <see cref="SetSizeLimit"/>).
        /// </summary>
        public void ResetSizeCounter()
        {
            totalBytesRetired = 0;
        }
        #endregion

        #region Validation
        /// <summary>
        /// Verifies that the last call to ReadTag() returned the given tag value.
        /// This is used to verify that a nested group ended with the correct
        /// end tag.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">The last
        /// tag read was not the one specified</exception>
        internal void CheckLastTagWas(uint value)
        {
            if (lastTag != value)
            {
                throw InvalidProtocolBufferException.InvalidEndTag();
            }
        }
        #endregion

        #region Reading of tags etc

        /// <summary>
        /// Attempts to peek at the next field tag.
        /// </summary>
        public bool PeekNextTag(out uint fieldTag)
        {
            if (hasNextTag)
            {
                fieldTag = nextTag;
                return true;
            }

            uint savedLast = lastTag;
            hasNextTag = ReadTag(out nextTag);
            lastTag = savedLast;
            fieldTag = nextTag;
            return hasNextTag;
        }

        /// <summary>
        /// Attempts to read a field tag, returning false if we have reached the end
        /// of the input data.
        /// </summary>
        /// <param name="fieldTag">The 'tag' of the field (id * 8 + wire-format)</param>
        /// <returns>true if the next fieldTag was read</returns>
        public bool ReadTag(out uint fieldTag)
        {
            if (hasNextTag)
            {
                fieldTag = nextTag;
                lastTag = fieldTag;
                hasNextTag = false;
                return true;
            }

            // Optimize for the incredibly common case of having at least two bytes left in the buffer,
            // and those two bytes being enough to get the tag. This will be true for fields up to 4095.
            if (bufferPos + 2 <= bufferSize)
            {
                int tmp = buffer[bufferPos++];
                if (tmp < 128)
                {
                    fieldTag = (uint)tmp;
                }
                else
                {
                    int result = tmp & 0x7f;
                    if ((tmp = buffer[bufferPos++]) < 128)
                    {
                        result |= tmp << 7;
                        fieldTag = (uint) result;
                    }
                    else
                    {
                        // Nope, rewind and go the potentially slow route.
                        bufferPos -= 2;
                        fieldTag = ReadRawVarint32();
                    }
                }
            }
            else
            {
                if (IsAtEnd)
                {
                    fieldTag = 0;
                    lastTag = fieldTag;
                    return false;
                }

                fieldTag = ReadRawVarint32();
            }
            lastTag = fieldTag;
            if (lastTag == 0)
            {
                // If we actually read zero, that's not a valid tag.
                throw InvalidProtocolBufferException.InvalidTag();
            }
            return true;
        }

        /// <summary>
        /// Consumes the data for the field with the tag we've just read.
        /// This should be called directly after <see cref="ReadTag"/>, when
        /// the caller wishes to skip an unknown field.
        /// </summary>
        public void ConsumeLastField()
        {
            if (lastTag == 0)
            {
                throw new InvalidOperationException("ConsumeLastField cannot be called at the end of a stream");
            }
            switch (WireFormat.GetTagWireType(lastTag))
            {
                case WireFormat.WireType.StartGroup:
                case WireFormat.WireType.EndGroup:
                    // TODO: Work out how to skip them instead? See issue 688.
                    throw new InvalidProtocolBufferException("Group tags not supported by proto3 C# implementation");
                case WireFormat.WireType.Fixed32:
                    ReadFixed32();
                    break;
                case WireFormat.WireType.Fixed64:
                    ReadFixed64();
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var length = ReadLength();
                    SkipRawBytes(length);
                    break;
                case WireFormat.WireType.Varint:
                    ReadRawVarint32();
                    break;
            }
        }

        /// <summary>
        /// Reads a double field from the stream.
        /// </summary>
        public double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble((long) ReadRawLittleEndian64());
        }

        /// <summary>
        /// Reads a float field from the stream.
        /// </summary>
        public float ReadFloat()
        {
            if (BitConverter.IsLittleEndian && 4 <= bufferSize - bufferPos)
            {
                float ret = BitConverter.ToSingle(buffer, bufferPos);
                bufferPos += 4;
                return ret;
            }
            else
            {
                byte[] rawBytes = ReadRawBytes(4);
                if (!BitConverter.IsLittleEndian)
                {
                    ByteArray.Reverse(rawBytes);
                }
                return BitConverter.ToSingle(rawBytes, 0);
            }
        }

        /// <summary>
        /// Reads a uint64 field from the stream.
        /// </summary>
        public ulong ReadUInt64()
        {
            return ReadRawVarint64();
        }

        /// <summary>
        /// Reads an int64 field from the stream.
        /// </summary>
        public long ReadInt64()
        {
            return (long) ReadRawVarint64();
        }

        /// <summary>
        /// Reads an int32 field from the stream.
        /// </summary>
        public int ReadInt32()
        {
            return (int) ReadRawVarint32();
        }

        /// <summary>
        /// Reads a fixed64 field from the stream.
        /// </summary>
        public ulong ReadFixed64()
        {
            return ReadRawLittleEndian64();
        }

        /// <summary>
        /// Reads a fixed32 field from the stream.
        /// </summary>
        public uint ReadFixed32()
        {
            return ReadRawLittleEndian32();
        }

        /// <summary>
        /// Reads a bool field from the stream.
        /// </summary>
        public bool ReadBool()
        {
            return ReadRawVarint32() != 0;
        }

        /// <summary>
        /// Reads a string field from the stream.
        /// </summary>
        public string ReadString()
        {
            int length = ReadLength();
            // No need to read any data for an empty string.
            if (length == 0)
            {
                return "";
            }
            if (length <= bufferSize - bufferPos)
            {
                // Fast path:  We already have the bytes in a contiguous buffer, so
                //   just copy directly from it.
                String result = CodedOutputStream.Utf8Encoding.GetString(buffer, bufferPos, length);
                bufferPos += length;
                return result;
            }
            // Slow path: Build a byte array first then copy it.
            return CodedOutputStream.Utf8Encoding.GetString(ReadRawBytes(length), 0, length);
        }

        /// <summary>
        /// Reads an embedded message field value from the stream.
        /// </summary>   
        public void ReadMessage(IMessage builder)
        {
            int length = ReadLength();
            if (recursionDepth >= recursionLimit)
            {
                throw InvalidProtocolBufferException.RecursionLimitExceeded();
            }
            int oldLimit = PushLimit(length);
            ++recursionDepth;
            builder.MergeFrom(this);
            CheckLastTagWas(0);
            --recursionDepth;
            PopLimit(oldLimit);
        }

        /// <summary>
        /// Reads a bytes field value from the stream.
        /// </summary>   
        public ByteString ReadBytes()
        {
            int length = ReadLength();
            if (length <= bufferSize - bufferPos && length > 0)
            {
                // Fast path:  We already have the bytes in a contiguous buffer, so
                //   just copy directly from it.
                ByteString result = ByteString.CopyFrom(buffer, bufferPos, length);
                bufferPos += length;
                return result;
            }
            else
            {
                // Slow path:  Build a byte array and attach it to a new ByteString.
                return ByteString.AttachBytes(ReadRawBytes(length));
            }
        }

        /// <summary>
        /// Reads a uint32 field value from the stream.
        /// </summary>   
        public uint ReadUInt32()
        {
            return ReadRawVarint32();
        }

        /// <summary>
        /// Reads an enum field value from the stream. If the enum is valid for type T,
        /// then the ref value is set and it returns true.  Otherwise the unknown output
        /// value is set and this method returns false.
        /// </summary>   
        public int ReadEnum()
        {
            // Currently just a pass-through, but it's nice to separate it logically from WriteInt32.
            return (int) ReadRawVarint32();
        }

        /// <summary>
        /// Reads an sfixed32 field value from the stream.
        /// </summary>   
        public int ReadSFixed32()
        {
            return (int) ReadRawLittleEndian32();
        }

        /// <summary>
        /// Reads an sfixed64 field value from the stream.
        /// </summary>   
        public long ReadSFixed64()
        {
            return (long) ReadRawLittleEndian64();
        }

        /// <summary>
        /// Reads an sint32 field value from the stream.
        /// </summary>   
        public int ReadSInt32()
        {
            return DecodeZigZag32(ReadRawVarint32());
        }

        /// <summary>
        /// Reads an sint64 field value from the stream.
        /// </summary>   
        public long ReadSInt64()
        {
            return DecodeZigZag64(ReadRawVarint64());
        }

        /// <summary>
        /// Reads a length for length-delimited data.
        /// </summary>
        /// <remarks>
        /// This is internally just reading a varint, but this method exists
        /// to make the calling code clearer.
        /// </remarks>
        public int ReadLength()
        {
            return (int) ReadRawVarint32();
        }

        /// <summary>
        /// Peeks at the next tag in the stream. If it matches <paramref name="tag"/>,
        /// the tag is consumed and the method returns <c>true</c>; otherwise, the
        /// stream is left in the original position and the method returns <c>false</c>.
        /// </summary>
        public bool MaybeConsumeTag(uint tag)
        {
            uint next;
            if (PeekNextTag(out next))
            {
                if (next == tag)
                {
                    hasNextTag = false;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Underlying reading primitives

        /// <summary>
        /// Same code as ReadRawVarint32, but read each byte individually, checking for
        /// buffer overflow.
        /// </summary>
        private uint SlowReadRawVarint32()
        {
            int tmp = ReadRawByte();
            if (tmp < 128)
            {
                return (uint) tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = ReadRawByte()) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = ReadRawByte()) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = ReadRawByte()) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = ReadRawByte()) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadRawByte() < 128)
                                {
                                    return (uint) result;
                                }
                            }
                            throw InvalidProtocolBufferException.MalformedVarint();
                        }
                    }
                }
            }
            return (uint) result;
        }

        /// <summary>
        /// Reads a raw Varint from the stream.  If larger than 32 bits, discard the upper bits.
        /// This method is optimised for the case where we've got lots of data in the buffer.
        /// That means we can check the size just once, then just read directly from the buffer
        /// without constant rechecking of the buffer length.
        /// </summary>
        internal uint ReadRawVarint32()
        {
            if (bufferPos + 5 > bufferSize)
            {
                return SlowReadRawVarint32();
            }

            int tmp = buffer[bufferPos++];
            if (tmp < 128)
            {
                return (uint) tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = buffer[bufferPos++]) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = buffer[bufferPos++]) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = buffer[bufferPos++]) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = buffer[bufferPos++]) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            // Note that this has to use ReadRawByte() as we only ensure we've
                            // got at least 5 bytes at the start of the method. This lets us
                            // use the fast path in more cases, and we rarely hit this section of code.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadRawByte() < 128)
                                {
                                    return (uint) result;
                                }
                            }
                            throw InvalidProtocolBufferException.MalformedVarint();
                        }
                    }
                }
            }
            return (uint) result;
        }

        /// <summary>
        /// Reads a varint from the input one byte at a time, so that it does not
        /// read any bytes after the end of the varint. If you simply wrapped the
        /// stream in a CodedInputStream and used ReadRawVarint32(Stream)
        /// then you would probably end up reading past the end of the varint since
        /// CodedInputStream buffers its input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static uint ReadRawVarint32(Stream input)
        {
            int result = 0;
            int offset = 0;
            for (; offset < 32; offset += 7)
            {
                int b = input.ReadByte();
                if (b == -1)
                {
                    throw InvalidProtocolBufferException.TruncatedMessage();
                }
                result |= (b & 0x7f) << offset;
                if ((b & 0x80) == 0)
                {
                    return (uint) result;
                }
            }
            // Keep reading up to 64 bits.
            for (; offset < 64; offset += 7)
            {
                int b = input.ReadByte();
                if (b == -1)
                {
                    throw InvalidProtocolBufferException.TruncatedMessage();
                }
                if ((b & 0x80) == 0)
                {
                    return (uint) result;
                }
            }
            throw InvalidProtocolBufferException.MalformedVarint();
        }

        /// <summary>
        /// Reads a raw varint from the stream.
        /// </summary>
        internal ulong ReadRawVarint64()
        {
            int shift = 0;
            ulong result = 0;
            while (shift < 64)
            {
                byte b = ReadRawByte();
                result |= (ulong) (b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
                shift += 7;
            }
            throw InvalidProtocolBufferException.MalformedVarint();
        }

        /// <summary>
        /// Reads a 32-bit little-endian integer from the stream.
        /// </summary>
        internal uint ReadRawLittleEndian32()
        {
            uint b1 = ReadRawByte();
            uint b2 = ReadRawByte();
            uint b3 = ReadRawByte();
            uint b4 = ReadRawByte();
            return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
        }

        /// <summary>
        /// Reads a 64-bit little-endian integer from the stream.
        /// </summary>
        internal ulong ReadRawLittleEndian64()
        {
            ulong b1 = ReadRawByte();
            ulong b2 = ReadRawByte();
            ulong b3 = ReadRawByte();
            ulong b4 = ReadRawByte();
            ulong b5 = ReadRawByte();
            ulong b6 = ReadRawByte();
            ulong b7 = ReadRawByte();
            ulong b8 = ReadRawByte();
            return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24)
                   | (b5 << 32) | (b6 << 40) | (b7 << 48) | (b8 << 56);
        }

        /// <summary>
        /// Decode a 32-bit value with ZigZag encoding.
        /// </summary>
        /// <remarks>
        /// ZigZag encodes signed integers into values that can be efficiently
        /// encoded with varint.  (Otherwise, negative values must be 
        /// sign-extended to 64 bits to be varint encoded, thus always taking
        /// 10 bytes on the wire.)
        /// </remarks>
        internal static int DecodeZigZag32(uint n)
        {
            return (int)(n >> 1) ^ -(int)(n & 1);
        }

        /// <summary>
        /// Decode a 32-bit value with ZigZag encoding.
        /// </summary>
        /// <remarks>
        /// ZigZag encodes signed integers into values that can be efficiently
        /// encoded with varint.  (Otherwise, negative values must be 
        /// sign-extended to 64 bits to be varint encoded, thus always taking
        /// 10 bytes on the wire.)
        /// </remarks>
        internal static long DecodeZigZag64(ulong n)
        {
            return (long)(n >> 1) ^ -(long)(n & 1);
        }
        #endregion

        #region Internal reading and buffer management

        /// <summary>
        /// Sets currentLimit to (current position) + byteLimit. This is called
        /// when descending into a length-delimited embedded message. The previous
        /// limit is returned.
        /// </summary>
        /// <returns>The old limit.</returns>
        internal int PushLimit(int byteLimit)
        {
            if (byteLimit < 0)
            {
                throw InvalidProtocolBufferException.NegativeSize();
            }
            byteLimit += totalBytesRetired + bufferPos;
            int oldLimit = currentLimit;
            if (byteLimit > oldLimit)
            {
                throw InvalidProtocolBufferException.TruncatedMessage();
            }
            currentLimit = byteLimit;

            RecomputeBufferSizeAfterLimit();

            return oldLimit;
        }

        private void RecomputeBufferSizeAfterLimit()
        {
            bufferSize += bufferSizeAfterLimit;
            int bufferEnd = totalBytesRetired + bufferSize;
            if (bufferEnd > currentLimit)
            {
                // Limit is in current buffer.
                bufferSizeAfterLimit = bufferEnd - currentLimit;
                bufferSize -= bufferSizeAfterLimit;
            }
            else
            {
                bufferSizeAfterLimit = 0;
            }
        }

        /// <summary>
        /// Discards the current limit, returning the previous limit.
        /// </summary>
        internal void PopLimit(int oldLimit)
        {
            currentLimit = oldLimit;
            RecomputeBufferSizeAfterLimit();
        }

        /// <summary>
        /// Returns whether or not all the data before the limit has been read.
        /// </summary>
        /// <returns></returns>
        internal bool ReachedLimit
        {
            get
            {
                if (currentLimit == int.MaxValue)
                {
                    return false;
                }
                int currentAbsolutePosition = totalBytesRetired + bufferPos;
                return currentAbsolutePosition >= currentLimit;
            }
        }

        /// <summary>
        /// Returns true if the stream has reached the end of the input. This is the
        /// case if either the end of the underlying input source has been reached or
        /// the stream has reached a limit created using PushLimit.
        /// </summary>
        public bool IsAtEnd
        {
            get { return bufferPos == bufferSize && !RefillBuffer(false); }
        }

        /// <summary>
        /// Called when buffer is empty to read more bytes from the
        /// input.  If <paramref name="mustSucceed"/> is true, RefillBuffer() gurantees that
        /// either there will be at least one byte in the buffer when it returns
        /// or it will throw an exception.  If <paramref name="mustSucceed"/> is false,
        /// RefillBuffer() returns false if no more bytes were available.
        /// </summary>
        /// <param name="mustSucceed"></param>
        /// <returns></returns>
        private bool RefillBuffer(bool mustSucceed)
        {
            if (bufferPos < bufferSize)
            {
                throw new InvalidOperationException("RefillBuffer() called when buffer wasn't empty.");
            }

            if (totalBytesRetired + bufferSize == currentLimit)
            {
                // Oops, we hit a limit.
                if (mustSucceed)
                {
                    throw InvalidProtocolBufferException.TruncatedMessage();
                }
                else
                {
                    return false;
                }
            }

            totalBytesRetired += bufferSize;

            bufferPos = 0;
            bufferSize = (input == null) ? 0 : input.Read(buffer, 0, buffer.Length);
            if (bufferSize < 0)
            {
                throw new InvalidOperationException("Stream.Read returned a negative count");
            }
            if (bufferSize == 0)
            {
                if (mustSucceed)
                {
                    throw InvalidProtocolBufferException.TruncatedMessage();
                }
                else
                {
                    return false;
                }
            }
            else
            {
                RecomputeBufferSizeAfterLimit();
                int totalBytesRead =
                    totalBytesRetired + bufferSize + bufferSizeAfterLimit;
                if (totalBytesRead > sizeLimit || totalBytesRead < 0)
                {
                    throw InvalidProtocolBufferException.SizeLimitExceeded();
                }
                return true;
            }
        }

        /// <summary>
        /// Read one byte from the input.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">
        /// the end of the stream or the current limit was reached
        /// </exception>
        internal byte ReadRawByte()
        {
            if (bufferPos == bufferSize)
            {
                RefillBuffer(true);
            }
            return buffer[bufferPos++];
        }

        /// <summary>
        /// Reads a fixed size of bytes from the input.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">
        /// the end of the stream or the current limit was reached
        /// </exception>
        internal byte[] ReadRawBytes(int size)
        {
            if (size < 0)
            {
                throw InvalidProtocolBufferException.NegativeSize();
            }

            if (totalBytesRetired + bufferPos + size > currentLimit)
            {
                // Read to the end of the stream (up to the current limit) anyway.
                // TODO(jonskeet): This is the only usage of SkipRawBytes. Do we really need to do it?
                SkipRawBytes(currentLimit - totalBytesRetired - bufferPos);
                // Then fail.
                throw InvalidProtocolBufferException.TruncatedMessage();
            }

            if (size <= bufferSize - bufferPos)
            {
                // We have all the bytes we need already.
                byte[] bytes = new byte[size];
                ByteArray.Copy(buffer, bufferPos, bytes, 0, size);
                bufferPos += size;
                return bytes;
            }
            else if (size < buffer.Length)
            {
                // Reading more bytes than are in the buffer, but not an excessive number
                // of bytes.  We can safely allocate the resulting array ahead of time.

                // First copy what we have.
                byte[] bytes = new byte[size];
                int pos = bufferSize - bufferPos;
                ByteArray.Copy(buffer, bufferPos, bytes, 0, pos);
                bufferPos = bufferSize;

                // We want to use RefillBuffer() and then copy from the buffer into our
                // byte array rather than reading directly into our byte array because
                // the input may be unbuffered.
                RefillBuffer(true);

                while (size - pos > bufferSize)
                {
                    Buffer.BlockCopy(buffer, 0, bytes, pos, bufferSize);
                    pos += bufferSize;
                    bufferPos = bufferSize;
                    RefillBuffer(true);
                }

                ByteArray.Copy(buffer, 0, bytes, pos, size - pos);
                bufferPos = size - pos;

                return bytes;
            }
            else
            {
                // The size is very large.  For security reasons, we can't allocate the
                // entire byte array yet.  The size comes directly from the input, so a
                // maliciously-crafted message could provide a bogus very large size in
                // order to trick the app into allocating a lot of memory.  We avoid this
                // by allocating and reading only a small chunk at a time, so that the
                // malicious message must actually *be* extremely large to cause
                // problems.  Meanwhile, we limit the allowed size of a message elsewhere.

                // Remember the buffer markers since we'll have to copy the bytes out of
                // it later.
                int originalBufferPos = bufferPos;
                int originalBufferSize = bufferSize;

                // Mark the current buffer consumed.
                totalBytesRetired += bufferSize;
                bufferPos = 0;
                bufferSize = 0;

                // Read all the rest of the bytes we need.
                int sizeLeft = size - (originalBufferSize - originalBufferPos);
                List<byte[]> chunks = new List<byte[]>();

                while (sizeLeft > 0)
                {
                    byte[] chunk = new byte[Math.Min(sizeLeft, buffer.Length)];
                    int pos = 0;
                    while (pos < chunk.Length)
                    {
                        int n = (input == null) ? -1 : input.Read(chunk, pos, chunk.Length - pos);
                        if (n <= 0)
                        {
                            throw InvalidProtocolBufferException.TruncatedMessage();
                        }
                        totalBytesRetired += n;
                        pos += n;
                    }
                    sizeLeft -= chunk.Length;
                    chunks.Add(chunk);
                }

                // OK, got everything.  Now concatenate it all into one buffer.
                byte[] bytes = new byte[size];

                // Start by copying the leftover bytes from this.buffer.
                int newPos = originalBufferSize - originalBufferPos;
                ByteArray.Copy(buffer, originalBufferPos, bytes, 0, newPos);

                // And now all the chunks.
                foreach (byte[] chunk in chunks)
                {
                    Buffer.BlockCopy(chunk, 0, bytes, newPos, chunk.Length);
                    newPos += chunk.Length;
                }

                // Done.
                return bytes;
            }
        }

        /// <summary>
        /// Reads and discards <paramref name="size"/> bytes.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">the end of the stream
        /// or the current limit was reached</exception>
        private void SkipRawBytes(int size)
        {
            if (size < 0)
            {
                throw InvalidProtocolBufferException.NegativeSize();
            }

            if (totalBytesRetired + bufferPos + size > currentLimit)
            {
                // Read to the end of the stream anyway.
                SkipRawBytes(currentLimit - totalBytesRetired - bufferPos);
                // Then fail.
                throw InvalidProtocolBufferException.TruncatedMessage();
            }

            if (size <= bufferSize - bufferPos)
            {
                // We have all the bytes we need already.
                bufferPos += size;
            }
            else
            {
                // Skipping more bytes than are in the buffer.  First skip what we have.
                int pos = bufferSize - bufferPos;

                // ROK 5/7/2013 Issue #54: should retire all bytes in buffer (bufferSize)
                // totalBytesRetired += pos;
                totalBytesRetired += bufferSize;
                
                bufferPos = 0;
                bufferSize = 0;

                // Then skip directly from the InputStream for the rest.
                if (pos < size)
                {
                    if (input == null)
                    {
                        throw InvalidProtocolBufferException.TruncatedMessage();
                    }
                    SkipImpl(size - pos);
                    totalBytesRetired += size - pos;
                }
            }
        }

        /// <summary>
        /// Abstraction of skipping to cope with streams which can't really skip.
        /// </summary>
        private void SkipImpl(int amountToSkip)
        {
            if (input.CanSeek)
            {
                long previousPosition = input.Position;
                input.Position += amountToSkip;
                if (input.Position != previousPosition + amountToSkip)
                {
                    throw InvalidProtocolBufferException.TruncatedMessage();
                }
            }
            else
            {
                byte[] skipBuffer = new byte[Math.Min(1024, amountToSkip)];
                while (amountToSkip > 0)
                {
                    int bytesRead = input.Read(skipBuffer, 0, Math.Min(skipBuffer.Length, amountToSkip));
                    if (bytesRead <= 0)
                    {
                        throw InvalidProtocolBufferException.TruncatedMessage();
                    }
                    amountToSkip -= bytesRead;
                }
            }
        }

        #endregion
    }
}