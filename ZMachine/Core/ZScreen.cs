using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Class which manages the windows and screen
    /// </summary>
    public class ZScreen
    {
        private readonly ZWindow[] windows;
        private readonly IUserInterface ui;

        private string buffer = string.Empty;
        private int currentWindow;

        /// <summary>
        /// Creates a new z machine screen
        /// </summary>
        /// <param name="ui">user interface to display the screen to</param>
        /// <param name="windowCount">number of windows in the screen</param>
        public ZScreen(IUserInterface ui, int windowCount)
        {
            //Store ui
            this.ui = ui;

            //Create each window
            ZWindow[] windowArray = new ZWindow[windowCount];
            for(int i = 0; i < windowArray.Length; i++)
            {
                windowArray[i] = new ZWindow(this);
            }

            //Store array
            this.windows = windowArray;
        }

        /// <summary>Gets the user interface used by this screen</summary>
        public IUserInterface UI
        {
            get { return ui; }
        }

        /// <summary>Gets or sets the index of the current window</summary>
        public int CurrentWindowIndex
        {
            get { return currentWindow; }

            set
            {
                //Validate window number
                if(value < 0 || value >= windows.Length)
                {
                    throw new ArgumentOutOfRangeException("value", "invalid window number: " + value);
                }

                //Store number
                currentWindow = value;

                //TODO update ui details
            }
        }

        /// <summary>Gets the current window object</summary>
        /// <remarks>To set the current window, use CurrentWindowIndex</remarks>
        public ZWindow CurrentWindow
        {
            get { return windows[currentWindow]; }
        }

        /// <summary>
        /// Gets the window with the given index
        /// </summary>
        /// <param name="index">index of the window to get (can be -3 for the current window)</param>
        public ZWindow this[int index]
        {
            get
            {
                //What index?
                if(index == -3)
                {
                    //Get current window
                    return CurrentWindow;
                }
                else if(index >= 0 && index < windows.Length)
                {
                    //Get window index
                    return windows[index];
                }
                else
                {
                    throw new ArgumentOutOfRangeException("index", "invalid window number: " + index);
                }
            }
        }

        /// <summary>
        /// Prints a new line
        /// </summary>
        private void PrintNewLine()
        {
            //TODO print new line and do scrolling etc
        }

        /// <summary>
        /// Gets the number of units left on the current line in the given window
        /// </summary>
        /// <param name="wnd">window to test</param>
        /// <returns>number of units left</returns>
        private int UnitsLeft(ZWindow wnd)
        {
            return Math.Max(wnd.Width - wnd.RightMargin - wnd.XCursor, 0);
        }

        /// <summary>
        /// Prints a word (string where whitespace is not interpreted)
        /// </summary>
        /// <param name="word">word to print</param>
        /// <param name="width">word width</param>
        private void PrintWord(string word, int width)
        {
            //Get printing width and units left
            int unitsLeft = UnitsLeft(CurrentWindow);

            //Enough space?
            if (width >= unitsLeft)
            {
                //Print word
                ui.PrintString(word);

                //Advance cursor
                //TODO do not update os cursor
                CurrentWindow.XCursor += width;
            }
            else if (width == unitsLeft)
            {
                //Print word
                ui.PrintString(word);

                //New line
                PrintNewLine();
            }
            else
            {
                //At the start of a line?
                if(CurrentWindow.XCursor == CurrentWindow.LeftMargin)
                {
                    //Yes, do character wrapping
                }
                else
                {
                    //Do new line first and then print word recursivly
                    PrintNewLine();
                    PrintWord(word, width);
                }
            }
        }

        /// <summary>
        /// Prints a string to the current window
        /// </summary>
        /// <param name="str">string to print</param>
        public void Print(string str)
        {
            string myBuffer = buffer;

            //Find whitespace in the string
            int wordStart = -1;
                //-1 wordStart means the work starts in the buffer

            for (int i = 0; i < str.Length; i++)
            {
                //Whitespace?
                if (Char.IsWhiteSpace(str, i))
                {
                    //New line?
                    if (str[i] == '\n')
                    {
                        //Do new line
                        PrintNewLine();
                    }
                    else
                    {
                        //Get word
                        string word;
                        if (wordStart == -1)
                            word = myBuffer + str.Substring(0, i);
                        else
                            word = str.Substring(wordStart, wordStart - i);

                        //Print word
                        PrintWord(word, ui.StringWidth(word));

                        //Print space
                        if (CurrentWindow.XCursor != CurrentWindow.LeftMargin)
                        {
                            //TODO could this cause a new line needlessly?
                            string spaceStr = new string(str[i], 1);
                            PrintWord(spaceStr, ui.StringWidth(spaceStr));
                        }
                    }
                }
            }

            //Place all unhandled characters in the buffer
            if (wordStart == -1)
            {
                //Add everything
                buffer += str;
            }
            else
            {
                //Add the rest of the string
                buffer = str.Substring(wordStart);
            }
        }

        /// <summary>
        /// Prints a string and a new line the current window
        /// </summary>
        /// <param name="str">string to print</param>
        public void PrintLine(string str)
        {
            Print(str);
            PrintNewLine();
        }

        /// <summary>
        /// Flushes the buffer of the current window
        /// </summary>
        public void Flush()
        {
        }
    }
}
