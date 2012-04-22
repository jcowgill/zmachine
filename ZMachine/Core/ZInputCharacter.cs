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
        public char UnicodeChar { get; set; }

        /// <summary>
        /// ZSCII character version (0 if unavaliable)
        /// </summary>
        public byte ZsciiChar { get; set; }

        /// <summary>
        /// Creates a new input character from a special ZSCII character
        /// </summary>
        /// <param name="zsciiChar">special ZSCII character</param>
        /// <remarks>
        /// You should use the unicode version of the constructor if this isn't a special character
        /// </remarks>
        public ZInputCharacter(byte zsciiChar) : this()
        {
            ZsciiChar = zsciiChar;
            UnicodeChar = '\0';
        }

        /// <summary>
        /// Creates a new input character from a unicode character
        /// </summary>
        /// <param name="unicodeChar">unicode character</param>
        public ZInputCharacter(char unicodeChar) : this()
        {
            ZsciiChar = 0;
            UnicodeChar = unicodeChar;
        }

        public override bool Equals(object obj)
        {
            //Attempt to unbox object
            if (obj is ZInputCharacter)
            {
                ZInputCharacter inChar = (ZInputCharacter) obj;

                //Test equality
                return UnicodeChar == inChar.UnicodeChar && ZsciiChar == inChar.ZsciiChar;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return 37 * (17 * 37 + (int) UnicodeChar) + ZsciiChar;
        }

        public static bool operator ==(ZInputCharacter a, ZInputCharacter b)
        {
            return a.ZsciiChar == b.ZsciiChar && a.UnicodeChar == b.UnicodeChar;
        }

        public static bool operator !=(ZInputCharacter a, ZInputCharacter b)
        {
            return a.ZsciiChar != b.ZsciiChar || a.UnicodeChar != b.UnicodeChar;
        }
    }
}
