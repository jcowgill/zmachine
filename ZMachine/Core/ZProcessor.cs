using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// A generic z-machine processor
    /// </summary>
    /// <remarks>
    /// This class contains common features of the z processor which apply to every version.
    /// </remarks>
    public abstract class ZProcessor
    {
        private readonly MemoryBuffer memBuf;

        /// <summary>
        /// Gets the memory buffer used by the processor
        /// </summary>
        public MemoryBuffer Memory { get { return memBuf; } }

        /// <summary>
        /// Creates a z processor with the given memory buffer
        /// </summary>
        /// <param name="buf">memory buffer</param>
        public ZProcessor(MemoryBuffer buf)
        {
            memBuf = buf;
        }

        /// <summary>
        /// Translates the routine packed address provided into a byte address
        /// </summary>
        /// <param name="packedAddr">packed address to convert</param>
        protected abstract int TranslateRoutineAddress(ushort packedAddr);

        /// <summary>
        /// Translates the packed string address provided into a byte address
        /// </summary>
        /// <param name="packedAddr">packed address to convert</param>
        protected virtual int TranslatePackedStrAddress(ushort packedAddr)
        {
            return TranslateRoutineAddress(packedAddr);
        }
    }
}
