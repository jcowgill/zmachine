using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Stores character inputs to the z machine (including special characters)
    /// </summary>
    public struct ZInputCharacter
    {
        /// <summary>
        /// Unicode character version (0 if unavaliable)
        /// </summary>
        public char unicodeChar;

        /// <summary>
        /// ZSCII character version (0 if unavaliable)
        /// </summary>
        public byte zsciiChar;

        /// <summary>
        /// Creates a new input character from a special ZSCII character
        /// </summary>
        /// <param name="zsciiChar">special ZSCII character</param>
        /// <remarks>
        /// You should use the unicode version of the constructor if this isn't a special character
        /// </remarks>
        public ZInputCharacter(byte zsciiChar)
        {
            this.zsciiChar = zsciiChar;
            this.unicodeChar = '\0';
        }

        /// <summary>
        /// Creates a new input character from a unicode character
        /// </summary>
        /// <param name="unicodeChar">unicode character</param>
        public ZInputCharacter(char unicodeChar)
        {
            this.zsciiChar = 0;
            this.unicodeChar = unicodeChar;
        }
    }
}
