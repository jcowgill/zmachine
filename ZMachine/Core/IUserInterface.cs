using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Provides methods which the processor uses to access the rest of the world
    /// </summary>
    /// <remarks>
    /// <para>The screen is measured in "units". These can be defined by
    /// the user interface and can be different horizintally and vertically.
    /// They must stay the same throughout the program however.</para>
    /// </remarks>
    public interface IUserInterface
    {
        /// <summary>
        /// Returns the width of the given string (in units) using the current font
        /// </summary>
        /// <param name="str">string to test</param>
        /// <returns>the width of the string in units</returns>
        int StringWidth(string str);

        /// <summary>
        /// Prints a string to the screen at the current cursor position
        /// </summary>
        /// <param name="string">string to print</param>
        /// <remarks>
        /// <para>When this method returns, the cursor should have advanced to the position after this string.
        /// This cursor MUST advance the same amount as would be reported by StringWidth.</para>
        /// <para>The cursor must not change y position (must not move to the next line)</para>
        /// </remarks>
        void PrintString(string str);

        /// <summary>
        /// Sets the cursor position (in units)
        /// </summary>
        /// <param name="x">x position in units (left = 0)</param>
        /// <param name="y">y position in units (top = 0)</param>
        void SetCursor(int x, int y);

        /// <summary>
        /// Scrolls an area of the screen upwards one line
        /// </summary>
        /// <param name="x">x coordinate of the top left corner of the region</param>
        /// <param name="y">y coordinate of the top left corner of the region</param>
        /// <param name="width">width of region</param>
        /// <param name="height">height of region</param>
        void ScrollArea(int x, int y, int width, int height);
    }
}
