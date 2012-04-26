using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Class containg a snapshot of the modifiable state of the ZMachine
    /// </summary>
    [Serializable]
    public class ZSnapshot
    {
        private readonly byte[] dynamicMem;
        private readonly ushort[] stack;
        private readonly int framePtr;
        private readonly int frameCount;

        /// <summary>
        /// Gets the frame pointer stored in this snapshot
        /// </summary>
        public int FramePointer { get { return framePtr; } }

        /// <summary>
        /// Gets the number of frames stored on the stack
        /// </summary>
        public int FramesCount { get { return frameCount; } }

        /// <summary>
        /// Gets the stack pointer stored in this snapshot
        /// </summary>
        public int StackPointer { get { return stack.Length; } }

        /// <summary>
        /// Gets the dynamic limit of the memory stored
        /// </summary>
        public int DynamicLimit { get { return dynamicMem.Length; } }

        /// <summary>
        /// Restores the stack data into the given array
        /// </summary>
        /// <param name="outputStack">output stack</param>
        public void RestoreStackData(ushort[] outputStack)
        {
            //Check array length
            if (outputStack.Length < stack.Length)
            {
                throw new ArgumentException("outputStack is not large enough");
            }

            //Copy stack data
            Array.Copy(stack, outputStack, stack.Length);
        }

        /// <summary>
        /// Restores the dynamic memory data into the given array
        /// </summary>
        /// <param name="outputData">output data</param>
        public void RestoreMemory(byte[] outputData)
        {
            //Check array length
            if (outputData.Length < dynamicMem.Length)
            {
                throw new ArgumentException("outputData is not large enough");
            }

            //Copy memory data
            Array.Copy(dynamicMem, outputData, dynamicMem.Length);
        }

        /// <summary>
        /// Restores the dynamic memory data into the given memory buffer
        /// </summary>
        /// <param name="buffer">buffer to restore data to</param>
        public void RestoreMemory(MemoryBuffer buffer)
        {
            //Validate buffer
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            else if (buffer.DynamicLimit != dynamicMem.Length)
            {
                throw new ArgumentException("buffer dynamic limit must equal the size of stored memory image");
            }

            //Copy memory data
            Array.Copy(dynamicMem, buffer.RawData, dynamicMem.Length);
        }

        /// <summary>
        /// Creates a new snapshot using the given data
        /// </summary>
        /// <param name="dynamicMemory">immutable array containing dynamic memory</param>
        /// <param name="stack">array containing stack data</param>
        /// <param name="framePtr">current stack frame pointer</param>
        /// <param name="frameCount">number of frames stored on the stack</param>
        /// <remarks>
        /// None of the parameters are copied. The caller must ensure they are not modified.
        /// </remarks>
        public ZSnapshot(byte[] dynamicMemory, ushort[] stack, int framePtr, int frameCount)
        {
            //Validate frame pointer
            if (framePtr > stack.Length)
            {
                throw new ArgumentOutOfRangeException("framePtr");
            }
            
            //Save arrays
            this.stack = stack;
            this.framePtr = framePtr;
            this.dynamicMem = dynamicMemory;
            this.frameCount = frameCount;
        }

        /// <summary>
        /// Creates a new snapshot using the given data
        /// </summary>
        /// <param name="dynamicMemory">memory buffer to copy memory from</param>
        /// <param name="stack">array to copy stack data from</param>
        /// <param name="framePtr">current stack frame pointer</param>
        /// <param name="stackPtr">current stack pointer</param>
        /// <param name="frameCount">number of frames stored on the stack</param>
        public ZSnapshot(MemoryBuffer buffer, ushort[] stack, int framePtr, int stackPtr, int frameCount)
        {
            //Validate frame pointers
            if (stackPtr > stack.Length)
            {
                throw new ArgumentOutOfRangeException("stackPtr");
            }
            else if (framePtr > stack.Length)
            {
                throw new ArgumentOutOfRangeException("framePtr");
            }

            //Copy memory
            this.framePtr = framePtr;
            this.stack = new ushort[stackPtr];
            Array.Copy(stack, this.stack, stackPtr);
            this.dynamicMem = new byte[buffer.DynamicLimit];
            Array.Copy(buffer.RawData, this.dynamicMem, buffer.DynamicLimit);
        }
    }
}
