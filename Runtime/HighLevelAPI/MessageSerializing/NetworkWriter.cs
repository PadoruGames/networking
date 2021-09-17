using System;
using System.Text;
using UnityEngine;


namespace Padoru.Networking
{
    /// <summary>
    ///   <para>General purpose serializer for UNET (for serializing data to byte arrays).</para>
    /// </summary>
    public class NetworkWriter
    {
        private const int k_MaxStringLength = 32768;
        private NetBuffer m_Buffer;
        private static Encoding s_Encoding;
        private static byte[] s_StringWriteBuffer;
        private static UIntFloat s_FloatConverter;

        /// <summary>
        ///   <para>The current position of the internal buffer.</para>
        /// </summary>
        public short Position
        {
            get
            {
                return (short)this.m_Buffer.Position;
            }
        }

        /// <summary>
        ///   <para>Creates a new NetworkWriter object.</para>
        /// </summary>
        /// <param name="buffer">A buffer to write into. This is not copied.</param>
        public NetworkWriter()
        {
            this.m_Buffer = new NetBuffer();
            if (NetworkWriter.s_Encoding != null)
                return;
            NetworkWriter.s_Encoding = (Encoding)new UTF8Encoding();
            NetworkWriter.s_StringWriteBuffer = new byte[32768];
        }

        /// <summary>
        ///   <para>Creates a new NetworkWriter object.</para>
        /// </summary>
        /// <param name="buffer">A buffer to write into. This is not copied.</param>
        public NetworkWriter(byte[] buffer)
        {
            this.m_Buffer = new NetBuffer(buffer);
            if (NetworkWriter.s_Encoding != null)
                return;
            NetworkWriter.s_Encoding = (Encoding)new UTF8Encoding();
            NetworkWriter.s_StringWriteBuffer = new byte[32768];
        }

        /// <summary>
        ///   <para>Returns a copy of internal array of bytes the writer is using, it copies only the bytes used.</para>
        /// </summary>
        /// <returns>
        ///   <para>Copy of data used by the writer.</para>
        /// </returns>
        public byte[] ToArray()
        {
            byte[] numArray = new byte[this.m_Buffer.AsArraySegment().Count];
            Array.Copy((Array)this.m_Buffer.AsArraySegment().Array, (Array)numArray, this.m_Buffer.AsArraySegment().Count);
            return numArray;
        }

        /// <summary>
        ///   <para>Returns the internal array of bytes the writer is using. This is NOT a copy.</para>
        /// </summary>
        /// <returns>
        ///   <para>Internal buffer.</para>
        /// </returns>
        public byte[] AsArray()
        {
            return this.AsArraySegment().Array;
        }

        internal ArraySegment<byte> AsArraySegment()
        {
            return this.m_Buffer.AsArraySegment();
        }

        /// <summary>
        ///   <para>This writes the 32-bit value to the stream using variable-length-encoding.</para>
        /// </summary>
        /// <param name="value">Value to write.</param>
        public void WritePackedUInt32(uint value)
        {
            if (value <= 240U)
                this.Write((byte)value);
            else if (value <= 2287U)
            {
                this.Write((byte)((value - 240U) / 256U + 241U));
                this.Write((byte)((value - 240U) % 256U));
            }
            else if (value <= 67823U)
            {
                this.Write((byte)249);
                this.Write((byte)((value - 2288U) / 256U));
                this.Write((byte)((value - 2288U) % 256U));
            }
            else if (value <= 16777215U)
            {
                this.Write((byte)250);
                this.Write((byte)(value & (uint)byte.MaxValue));
                this.Write((byte)(value >> 8 & (uint)byte.MaxValue));
                this.Write((byte)(value >> 16 & (uint)byte.MaxValue));
            }
            else
            {
                this.Write((byte)251);
                this.Write((byte)(value & (uint)byte.MaxValue));
                this.Write((byte)(value >> 8 & (uint)byte.MaxValue));
                this.Write((byte)(value >> 16 & (uint)byte.MaxValue));
                this.Write((byte)(value >> 24 & (uint)byte.MaxValue));
            }
        }

        /// <summary>
        ///   <para>This writes the 64-bit value to the stream using variable-length-encoding.</para>
        /// </summary>
        /// <param name="value">Value to write.</param>
        public void WritePackedUInt64(ulong value)
        {
            if (value <= 240UL)
                this.Write((byte)value);
            else if (value <= 2287UL)
            {
                this.Write((byte)((value - 240UL) / 256UL + 241UL));
                this.Write((byte)((value - 240UL) % 256UL));
            }
            else if (value <= 67823UL)
            {
                this.Write((byte)249);
                this.Write((byte)((value - 2288UL) / 256UL));
                this.Write((byte)((value - 2288UL) % 256UL));
            }
            else if (value <= 16777215UL)
            {
                this.Write((byte)250);
                this.Write((byte)(value & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 8 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 16 & (ulong)byte.MaxValue));
            }
            else if (value <= (ulong)uint.MaxValue)
            {
                this.Write((byte)251);
                this.Write((byte)(value & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 8 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 16 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 24 & (ulong)byte.MaxValue));
            }
            else if (value <= 1099511627775UL)
            {
                this.Write((byte)252);
                this.Write((byte)(value & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 8 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 16 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 24 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 32 & (ulong)byte.MaxValue));
            }
            else if (value <= 281474976710655UL)
            {
                this.Write((byte)253);
                this.Write((byte)(value & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 8 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 16 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 24 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 32 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 40 & (ulong)byte.MaxValue));
            }
            else if (value <= 72057594037927935UL)
            {
                this.Write((byte)254);
                this.Write((byte)(value & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 8 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 16 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 24 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 32 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 40 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 48 & (ulong)byte.MaxValue));
            }
            else
            {
                this.Write(byte.MaxValue);
                this.Write((byte)(value & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 8 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 16 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 24 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 32 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 40 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 48 & (ulong)byte.MaxValue));
                this.Write((byte)(value >> 56 & (ulong)byte.MaxValue));
            }
        }

        public void Write(char value)
        {
            this.m_Buffer.WriteByte((byte)value);
        }

        public void Write(byte value)
        {
            this.m_Buffer.WriteByte(value);
        }

        public void Write(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                Write(bytes[i]);
            }
        }

        public void Write(sbyte value)
        {
            this.m_Buffer.WriteByte((byte)value);
        }

        public void Write(short value)
        {
            this.m_Buffer.WriteByte2((byte)((uint)value & (uint)byte.MaxValue), (byte)((int)value >> 8 & (int)byte.MaxValue));
        }

        public void Write(ushort value)
        {
            this.m_Buffer.WriteByte2((byte)((uint)value & (uint)byte.MaxValue), (byte)((int)value >> 8 & (int)byte.MaxValue));
        }

        public void Write(int value)
        {
            this.m_Buffer.WriteByte4((byte)(value & (int)byte.MaxValue), (byte)(value >> 8 & (int)byte.MaxValue), (byte)(value >> 16 & (int)byte.MaxValue), (byte)(value >> 24 & (int)byte.MaxValue));
        }

        public void Write(uint value)
        {
            this.m_Buffer.WriteByte4((byte)(value & (uint)byte.MaxValue), (byte)(value >> 8 & (uint)byte.MaxValue), (byte)(value >> 16 & (uint)byte.MaxValue), (byte)(value >> 24 & (uint)byte.MaxValue));
        }

        public void Write(long value)
        {
            this.m_Buffer.WriteByte8((byte)((ulong)value & (ulong)byte.MaxValue), (byte)((ulong)(value >> 8) & (ulong)byte.MaxValue), (byte)((ulong)(value >> 16) & (ulong)byte.MaxValue), (byte)((ulong)(value >> 24) & (ulong)byte.MaxValue), (byte)((ulong)(value >> 32) & (ulong)byte.MaxValue), (byte)((ulong)(value >> 40) & (ulong)byte.MaxValue), (byte)((ulong)(value >> 48) & (ulong)byte.MaxValue), (byte)((ulong)(value >> 56) & (ulong)byte.MaxValue));
        }

        public void Write(ulong value)
        {
            this.m_Buffer.WriteByte8((byte)(value & (ulong)byte.MaxValue), (byte)(value >> 8 & (ulong)byte.MaxValue), (byte)(value >> 16 & (ulong)byte.MaxValue), (byte)(value >> 24 & (ulong)byte.MaxValue), (byte)(value >> 32 & (ulong)byte.MaxValue), (byte)(value >> 40 & (ulong)byte.MaxValue), (byte)(value >> 48 & (ulong)byte.MaxValue), (byte)(value >> 56 & (ulong)byte.MaxValue));
        }

        public void Write(float value)
        {
            NetworkWriter.s_FloatConverter.floatValue = value;
            this.Write(NetworkWriter.s_FloatConverter.intValue);
        }

        public void Write(double value)
        {
            NetworkWriter.s_FloatConverter.doubleValue = value;
            this.Write(NetworkWriter.s_FloatConverter.longValue);
        }

        public void Write(string value)
        {
            if (value == null)
            {
                this.m_Buffer.WriteByte2((byte)0, (byte)0);
            }
            else
            {
                int byteCount = NetworkWriter.s_Encoding.GetByteCount(value);
                if (byteCount >= 32768)
                    throw new IndexOutOfRangeException("Serialize(string) too long: " + (object)value.Length);
                this.Write((ushort)byteCount);
                int bytes = NetworkWriter.s_Encoding.GetBytes(value, 0, value.Length, NetworkWriter.s_StringWriteBuffer, 0);
                this.m_Buffer.WriteBytes(NetworkWriter.s_StringWriteBuffer, (ushort)bytes);
            }
        }

        public void Write(bool value)
        {
            if (value)
                this.m_Buffer.WriteByte((byte)1);
            else
                this.m_Buffer.WriteByte((byte)0);
        }

        public void Write(byte[] buffer, int count)
        {
            if (count > (int)ushort.MaxValue)
            {
                Debug.LogError((object)("NetworkWriter Write: buffer is too large (" + (object)count + ") bytes. The maximum buffer size is 64K bytes."));
            }
            else
                this.m_Buffer.WriteBytes(buffer, (ushort)count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (count > (int)ushort.MaxValue)
            {
                Debug.LogError((object)("NetworkWriter Write: buffer is too large (" + (object)count + ") bytes. The maximum buffer size is 64K bytes."));
            }
            else
                this.m_Buffer.WriteBytesAtOffset(buffer, (ushort)offset, (ushort)count);
        }

        /// <summary>
        ///   <para>This writes a 16-bit count and a array of bytes of that length to the stream.</para>
        /// </summary>
        /// <param name="buffer">Array of bytes to write.</param>
        /// <param name="count">Number of bytes from the array to write.</param>
        public void WriteBytesAndSize(byte[] buffer, int count)
        {
            if (buffer == null || count == 0)
                this.Write((ushort)0);
            else if (count > (int)ushort.MaxValue)
            {
                Debug.LogError((object)("NetworkWriter WriteBytesAndSize: buffer is too large (" + (object)count + ") bytes. The maximum buffer size is 64K bytes."));
            }
            else
            {
                this.Write((ushort)count);
                this.m_Buffer.WriteBytes(buffer, (ushort)count);
            }
        }

        /// <summary>
        ///   <para>This writes a 16-bit count and an array of bytes of that size to the stream.</para>
        /// </summary>
        /// <param name="buffer">Bytes to write.</param>
        public void WriteBytesFull(byte[] buffer)
        {
            if (buffer == null)
                this.Write((ushort)0);
            else if (buffer.Length > (int)ushort.MaxValue)
            {
                Debug.LogError((object)("NetworkWriter WriteBytes: buffer is too large (" + (object)buffer.Length + ") bytes. The maximum buffer size is 64K bytes."));
            }
            else
            {
                this.Write((ushort)buffer.Length);
                this.m_Buffer.WriteBytes(buffer, (ushort)buffer.Length);
            }
        }

        public void Write(Vector2 value)
        {
            this.Write(value.x);
            this.Write(value.y);
        }

        public void Write(Vector3 value)
        {
            this.Write(value.x);
            this.Write(value.y);
            this.Write(value.z);
        }

        public void Write(Vector4 value)
        {
            this.Write(value.x);
            this.Write(value.y);
            this.Write(value.z);
            this.Write(value.w);
        }

        public void Write(Color value)
        {
            this.Write(value.r);
            this.Write(value.g);
            this.Write(value.b);
            this.Write(value.a);
        }

        public void Write(Color32 value)
        {
            this.Write(value.r);
            this.Write(value.g);
            this.Write(value.b);
            this.Write(value.a);
        }

        public void Write(Quaternion value)
        {
            this.Write(value.x);
            this.Write(value.y);
            this.Write(value.z);
            this.Write(value.w);
        }

        public void Write(Rect value)
        {
            this.Write(value.xMin);
            this.Write(value.yMin);
            this.Write(value.width);
            this.Write(value.height);
        }

        public void Write(Plane value)
        {
            this.Write(value.normal);
            this.Write(value.distance);
        }

        public void Write(Ray value)
        {
            this.Write(value.direction);
            this.Write(value.origin);
        }

        public void Write(Matrix4x4 value)
        {
            this.Write(value.m00);
            this.Write(value.m01);
            this.Write(value.m02);
            this.Write(value.m03);
            this.Write(value.m10);
            this.Write(value.m11);
            this.Write(value.m12);
            this.Write(value.m13);
            this.Write(value.m20);
            this.Write(value.m21);
            this.Write(value.m22);
            this.Write(value.m23);
            this.Write(value.m30);
            this.Write(value.m31);
            this.Write(value.m32);
            this.Write(value.m33);
        }

        /// <summary>
        ///   <para>Seeks to the start of the internal buffer.</para>
        /// </summary>
        public void SeekZero()
        {
            this.m_Buffer.SeekZero();
        }

        /// <summary>
        ///   <para>This begins a new message, which should be completed with FinishMessage() once the payload has been written.</para>
        /// </summary>
        /// <param name="msgType">Message type.</param>
        public void StartMessage(short msgType)
        {
            this.SeekZero();
            this.m_Buffer.WriteByte2((byte)0, (byte)0);
            this.Write(msgType);
        }

        /// <summary>
        ///   <para>This fills out the size header of a message begun with StartMessage(), so that it can be send using Send() functions.</para>
        /// </summary>
        public void FinishMessage()
        {
            this.m_Buffer.FinishMessage();
        }
    }
}