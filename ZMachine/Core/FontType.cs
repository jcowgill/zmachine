using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Defines the fonts avaliable to the z machine
    /// </summary>
    public enum FontType
    {
        /// <summary>
        /// The normal font
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Special font for displaying common graphics (see spec section 16)
        /// </summary>
        CharacterGraphicsFont = 3,

        /// <summary>
        /// A fixed width font
        /// </summary>
        FixedPitchFont = 4,
    }
}
