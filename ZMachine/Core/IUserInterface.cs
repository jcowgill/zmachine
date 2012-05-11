using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Provides methods which the processor uses to access the rest of the world
    /// </summary>
    public interface IUserInterface
    {
        /// <summary>
        /// Returns the width of the given character (in units) using the current font
        /// </summary>
        /// <param name="c">character to test</param>
        /// <returns>the width of the character in units</returns>
        int CharWidth(char c);

        /// <summary>
        /// Prints a character to the screen at the current cursor position
        /// </summary>
        /// <param name="c">character to print</param>
        /// <remarks>
        /// <para>The cursor should advance to the position after the character which has been printed.
        /// This MUST correspond with advancing the CharWidth of the given character.</para>
        /// <para>The cursor must not change y position (must not move to the next line)</para>
        /// </remarks>
        void PrintChar(char c);

        /// <summary>
        /// Sets the cursor position (in units)
        /// </summary>
        /// <param name="x">x position in units (left = 0)</param>
        /// <param name="y">y position in units (top = 0)</param>
        void SetCursor(int x, int y);
    }
}
