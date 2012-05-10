using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// A window in the z machine screen model
    /// </summary>
    /// <remarks>
    /// Windows are used to set how text is displayed in the z machine.
    /// Importantly, printed text never "belongs" to a window (eg moving a window does not move text).
    /// </remarks>
    public class ZWindow
    {
        //My screen
        private readonly ZScreen screen;

        //Private backing variables for SOME properties
        private int my_xPos;
        private int my_yPos;
        private int my_width;
        private int my_height;
        private int my_xCursor;
        private int my_yCursor;
        private int my_leftMargin;
        private int my_rightMargin;

        //Font styles, colours and sizes
        private TextStyle my_style;
        private FontType my_font;
        private int my_foreground;
        private int my_background;

        /// <summary>
        /// Creates a new window
        /// </summary>
        /// <param name="parentScreen">the screen the window belongs to</param>
        public ZWindow(ZScreen parentScreen)
        {
            this.screen = parentScreen;
        }

        /// <summary>Gets the screen this window belongs to</summary>
        public ZScreen Screen
        {
            get { return screen; }
        }

        /// <summary>X position of left of the window (in units - base 0)</summary>
        public int XPosition
        {
            get { return my_xPos; }
            set { Flush(); my_xPos = value; }
        }

        /// <summary>Y position of top of the window (in units - base 0)</summary>
        public int YPosition
        {
            get { return my_yPos; }
            set { Flush(); my_yPos = value; }
        }

        /// <summary>Width of the window in units</summary>
        public int Width
        {
            get { return my_width; }

            set
            {
                //Flush and set width
                Flush();
                my_width = value;

                //If the cursor is out of range, reset it
                if (XCursor >= Width)
                    ResetCursor();
            }
        }

        /// <summary>Height of the window in units</summary>
        public int Height
        {
            get { return my_height; }

            set
            {
                //Flush and set width
                Flush();
                my_height = value;

                //If the cursor is out of range, reset it
                if (YCursor >= Height)
                    ResetCursor();
            }
        }

        /// <summary>X position of cursor (in units - base 0)</summary>
        public int XCursor
        {
            get { return my_xCursor; }

            set
            {
                //Flush
                Flush();

                //Set cursor only if in range
                if (value < LeftMargin || value >= Width - RightMargin)
                {
                    my_xCursor = LeftMargin;
                }
                else
                {
                    my_xCursor = value;
                }

                //TODO if this is the current window, update ui cursor
            }
        }

        /// <summary>Y position of cursor (in units - base 0)</summary>
        public int YCursor
        {
            get { return my_yCursor; }

            set
            {
                //Flush
                Flush();

                //Set cursor only if in range
                if (value < 0 || value >= Height)
                {
                    my_yCursor = 0;
                }
                else
                {
                    my_yCursor = value;
                }

                //TODO if this is the current window, update ui cursor
            }
        }

        /// <summary>Width of left margin in units</summary>
        public int LeftMargin
        {
            get { return my_leftMargin; }

            set
            {
                //Flush and set margin
                Flush();
                my_leftMargin = value;

                //Set cursor if out of range
                if (XCursor < LeftMargin || XCursor >= Width - RightMargin)
                    my_xCursor = LeftMargin;
            }
        }

        /// <summary>Width of right margin in units</summary>
        public int RightMargin
        {
            get { return my_rightMargin; }

            set
            {
                //Flush and set margin
                Flush();
                my_rightMargin = value;

                //Set cursor if out of range
                if (XCursor < LeftMargin || XCursor >= Width - RightMargin)
                    my_xCursor = LeftMargin;
            }
        }

        /// <summary>The routine to be called when InterruptCountdown goes from 1 to 0/summary>
        /// <remarks>This is NOT a packed address</remarks>
        public int NewLineInterrupt { get; set; }

        /// <summary>The new line interrupt is called when this reaches 0</summary>
        public int InterruptCountdown { get; set; }

        /// <summary>Style of text to display</summary>
        public TextStyle Style
        {
            get { return my_style; }

            set
            {
                //Flush and set style
                Flush();
                my_style = value;

                //Validate
                if ((int) value < 0 || (int) value > 15)
                {
                    throw new ZMachineException("invalid property value for window text style: " + value);
                }

                //TODO cache font properties
            }
        }

        /// <summary>Font of text to display</summary>
        public FontType Font
        {
            get { return my_font; }

            set
            {
                //Flush and set style
                Flush();
                my_font = value;

                //Validate
                if (value != FontType.Normal || value != FontType.CharacterGraphicsFont ||
                        value != FontType.FixedPitchFont)
                {
                    throw new ZMachineException("invalid property value for window font style: " + value);
                }

                //TODO cache font properties
            }
        }

        /// <summary>RGB foreground colour of the text</summary>
        public int ForegroundColour
        {
            get { return my_foreground; }
            set { Flush(); my_foreground = value; }
        }

        /// <summary>RGB background colour of the text</summary>
        public int BackgroundColour
        {
            get { return my_background; }
            set { Flush(); my_background = value; }
        }

        /// <summary>
        /// Resets the cursor to its initial position
        /// </summary>
        private void ResetCursor()
        {
        }

        /// <summary>
        /// Prints any buffered text owned by this window
        /// </summary>
        public void Flush()
        {
            //Use screen flusher
            if (Screen.CurrentWindow == this)
            {
                Screen.Flush();
            }
        }
    }
}
