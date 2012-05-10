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
        /// Flushes the buffer of the current window
        /// </summary>
        public void Flush()
        {
        }
    }
}
