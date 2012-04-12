using System;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Z Machine Memory Buffer
    /// </summary>
    public class MemoryBuffer
    {
        private byte[] buf;
        private int dynamicLimit;

        /// <summary>
        /// Creates a new buffer using the given raw data backing
        /// </summary>
        /// <param name="data">raw data</param>
        public MemoryBuffer(byte[] data)
        {
            buf = data;
            dynamicLimit = data.Length;
        }

        /// <summary>
        /// Creates a new blank buffer of the given size
        /// </summary>
        /// <param name="length">buffer size</param>
        public MemoryBuffer(int length)
            : this(new byte[length])
        {
        }

        /// <summary>
        /// Gets the raw byte array used by the buffer
        /// </summary>
        public byte[] RawData
        {
            get
            {
                return buf;
            }
        }

        /// <summary>
        /// Returns the buffer's length
        /// </summary>
        public int Length
        {
            get
            {
                return buf.Length;
            }
        }

        /// <summary>
        /// Gets or sets the limit which set options can be used up to
        /// </summary>
        /// <remarks>
        /// <para>This property prevents sets to higher regions of memory (non-dynamic).</para>
        /// <para>Set this to the first byte of static memory to use this.</para>
        /// <para>This property defaults to the data length (ie allows any memory to be set).</para>
        /// </remarks>
        /// <exception cref="ZMachineException">Thrown if the requested limit is out of range</exception>
        public int DynamicLimit
        {
            get
            {
                return dynamicLimit;
            }

            set
            {
                //Must be in range
                if (value < 0 || value > Length)
                {
                    throw new ZMachineException("requested dynamic memory limit out of range");
                }

                dynamicLimit = value;
            }
        }

        /// <summary>
        /// Gets a byte from the buffer
        /// </summary>
        /// <param name="pos">position to get byte from</param>
        public byte GetByte(int pos)
        {
            if (pos >= buf.Length)
            {
                throw new ZMachineException("illegal \"get\" for memory address 0x" + pos.ToString("X8"));
            }

            return buf[pos];
        }

        /// <summary>
        /// Gets a short from the buffer
        /// </summary>
        /// <param name="pos">position to get short from</param>
        public short GetShort(int pos)
        {
            return (short) GetUShort(pos);
        }

        /// <summary>
        /// Gets an unsigned short from the buffer
        /// </summary>
        /// <param name="pos">position to get unsigned short from</param>
        public ushort GetUShort(int pos)
        {
            return (ushort) (GetByte(pos) << 8 | GetByte(pos + 1));
        }

        /// <summary>
        /// Gets an int from the buffer
        /// </summary>
        /// <param name="pos">position to get int from</param>
        public int GetInt(int pos)
        {
            return (int) GetUInt(pos);
        }

        /// <summary>
        /// Gets an unsigned int from the buffer
        /// </summary>
        /// <param name="pos">position to get unsigned int from</param>
        public uint GetUInt(int pos)
        {
            return (uint) (GetByte(pos) << 24 |
                           GetByte(pos + 1) << 16 |
                           GetByte(pos + 2) << 8 |
                           GetByte(pos + 3));
        }

        /// <summary>
        /// Sets a byte in the buffer
        /// </summary>
        /// <param name="pos">position of byte to set</param>
        /// <param name="data">byte to store</param>
        public void SetByte(int pos, byte data)
        {
            if (pos >= dynamicLimit)
            {
                throw new ZMachineException("illegal \"set\" for memory address 0x" + pos.ToString("X8"));
            }

            buf[pos] = data;
        }

        /// <summary>
        /// Sets a short in the buffer
        /// </summary>
        /// <param name="pos">position of short to set</param>
        /// <param name="data">short to store</param>
        public void SetShort(int pos, short data)
        {
            SetUShort(pos, (ushort) data);
        }

        /// <summary>
        /// Sets an unsigned short in the buffer
        /// </summary>
        /// <param name="pos">position of unsigned short to set</param>
        /// <param name="data">unsigned short to store</param>
        public void SetUShort(int pos, ushort data)
        {
            SetByte(pos, (byte) (data >> 8));
            SetByte(pos + 1, (byte) (data));
        }

        /// <summary>
        /// Sets an int in the buffer
        /// </summary>
        /// <param name="pos">position of int to set</param>
        /// <param name="data">int to store</param>
        public void SetInt(int pos, int data)
        {
            SetUInt(pos, (ushort) data);
        }

        /// <summary>
        /// Sets an unsigned int in the buffer
        /// </summary>
        /// <param name="pos">position of unsigned int to set</param>
        /// <param name="data">unsigned int to store</param>
        public void SetUInt(int pos, uint data)
        {
            SetByte(pos, (byte) (data >> 24));
            SetByte(pos + 1, (byte) (data >> 16));
            SetByte(pos + 2, (byte) (data >> 8));
            SetByte(pos + 3, (byte) data);
        }
    }
}
