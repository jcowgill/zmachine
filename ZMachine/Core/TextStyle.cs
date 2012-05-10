using System;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// The style of text printed to the screen
    /// </summary>
    [Flags]
    public enum TextStyle
    {
        /// <summary>
        /// Normal text
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Revserse the foreground and background colours
        /// </summary>
        ReverseVideo = 1,

        /// <summary>
        /// Bold text
        /// </summary>
        Bold = 2,

        /// <summary>
        /// Italic text
        /// </summary>
        Italic = 4,

        /// <summary>
        /// Fixed-width font
        /// </summary>
        FixedPitch = 8,
    }
}
