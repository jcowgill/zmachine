using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Encodes and Decodes strings used by the Z-Machine
    /// </summary>
    public sealed class ZCharacterEncoder
    {
        private readonly MemoryBuffer buf;
        private readonly char[] unicodeCache;
        private readonly char[] alphabetCache;
        private readonly string[] abbreviationCache;
        private readonly int machineVersion;

        /// <summary>
        /// Default unicode translation table (ZSCII 155 - 251)
        /// </summary>
        private static readonly char[] DEFAULT_UNICODE_TABLE = (
            "\u00e4\u00f6\u00fc\u00c4\u00d6\u00dc\u00df\u00bb\u00ab\u00eb\u00ef\u00ff\u00cb\u00e1\u00e9\u00ed" +
            "\u00f3\u00fa\u00fd\u00c1\u00c9\u00cd\u00d3\u00da\u00dd\u00e0\u00e8\u00ec\u00f2\u00f9\u00c0\u00c8" +
            "\u00cc\u00d2\u00d9\u00e2\u00ea\u00ee\u00f4\u00fb\u00c2\u00ca\u00ce\u00d4\u00db\u00e5\u00c5\u00f8" +
            "\u00d8\u00e3\u00f1\u00f5\u00c3\u00d1\u00d5\u00e6\u00e7\u00c7\u00fe\u00f0\u00de\u00d0\u00a3\u0153" +
            "\u0152\u00a1\u00bf").ToCharArray();

        /// <summary>
        /// Default alphabet table (v2-8)
        /// </summary>
        private static readonly char[] DEFAULT_ALPHABET_TABLE =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \n0123456789.,!?_#'\"/\\-:()".ToCharArray();

        /// <summary>
        /// Default alphabet table (v1)
        /// </summary>
        private static readonly char[] DEFAULT_ALPHABET_TABLE_V1 =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789.,!?_#'\"/\\<-:()".ToCharArray();

        /// <summary>
        /// Unicode replacement character - used for illegal ZSCII characters
        /// </summary>
        public const char INVALID_CHAR = '\uFFFD';

        /// <summary>
        /// Creates a new z machine character encoder
        /// </summary>
        /// <param name="buf">game memory buffer</param>
        public ZCharacterEncoder(MemoryBuffer buf)
        {
            //Store buffer
            this.buf = buf;

            //Get version
            this.machineVersion = buf.GetByte(0);

            //Find unicode table address
            int unicodeTable = 0;

            if (machineVersion >= 5)
            {
                //Search the extension header table
                ushort extensionHdr = buf.GetUShort(0x36);

                if (extensionHdr != 0 && buf.GetUShort(extensionHdr) >= 3)
                {
                    unicodeTable = buf.GetUShort(extensionHdr + 6);
                }
            }

            //Create unicode cache
            this.unicodeCache = CreateUnicodeCache(unicodeTable);

            //Create alphabet cache
            if (machineVersion >= 5 && buf.GetUShort(0x34) != 0)
            {
                this.alphabetCache = CreateAlphabetCache(buf.GetUShort(0x34));
            }
            else
            {
                //Use default alphabet
                this.alphabetCache = machineVersion == 1 ? DEFAULT_ALPHABET_TABLE_V1 : DEFAULT_ALPHABET_TABLE;
            }

            //Create abbreviation cache
            if (machineVersion >= 2 && buf.GetUShort(0x18) != 0)
            {
                this.abbreviationCache = CreateAbbreviationCache(buf.GetUShort(0x18));
            }
        }

        /// <summary>
        /// Creates the ZSCII -> Unicode cache
        /// </summary>
        /// <param name="unicodeTable">pointer to custom table or 0 to use default</param>
        /// <returns>cache containing an entry for all 256 characters</returns>
        private char[] CreateUnicodeCache(int unicodeTable)
        {
            //Create base unicode table
            char[] cache = new char[256];
            for (int i = 1; i < 0x20; i++)
            {
                cache[i] = INVALID_CHAR;
            }

            cache[0] = '\0';
            cache[9] = '\t';
            cache[11] = ' ';
            cache[13] = '\n';

            //Copy ASCII
            for (char i = ' '; i <= '~'; i++)
            {
                cache[i] = i;
            }

            //Wipe second section
            for (int i = 127; i < 256; i++)
            {
                cache[i] = INVALID_CHAR;
            }

            //Create custom unicode section
            if (unicodeTable == 0)
            {
                //Copy default table
                Array.Copy(DEFAULT_UNICODE_TABLE, 0, cache, 155, DEFAULT_UNICODE_TABLE.Length);
            }
            else
            {
                //Get number of characters in table
                byte tableChars = buf.GetByte(unicodeTable);

                //Copy characters
                int endPos = Math.Min(tableChars + 155, 252);
                int currTablePos = unicodeTable + 1;

                for (int i = 155; i < endPos; i++)
                {
                    cache[i] = buf.GetChar(currTablePos);
                    currTablePos += 2;
                }
            }

            return cache;
        }

        /// <summary>
        /// Creates a CUSTOM alphabet -> unicode cache
        /// </summary>
        /// <param name="alphabetTable">pointer to alphabet table to use</param>
        /// <returns>the new alphabet cache (contains 78 values)</returns>
        private char[] CreateAlphabetCache(int alphabetTable)
        {
            //The alphabet cache is just an array of zscii translations
            char[] alphabetCache = new char[78];

            for (int i = 0; i < 78; i++)
            {
                alphabetCache[i] = unicodeCache[buf.GetByte(alphabetTable + i)];
            }

            //Override Z character A2 7 to be a newline
            alphabetCache[2 * 26 + 1] = '\n';

            return alphabetCache;
        }

        /// <summary>
        /// Creates the cache of abbreviations
        /// </summary>
        /// <param name="abbrevTable">location of the abbreviation table</param>
        /// <returns>the new abbreviation cache</returns>
        private string[] CreateAbbreviationCache(int abbrevTable)
        {
            //Create cache array
            int strCount = machineVersion == 2 ? 32 : 96;
            string[] cache = new string[strCount];

            //Optimise for all the same pointers
            int firstPtr = buf.GetUShort(abbrevTable);
            cache[0] = Decode(firstPtr);

            //Process abbreviations
            for (int i = 1; i < strCount; i++)
            {
                //Get address
                int address = buf.GetUShort(abbrevTable + i * 2);

                //First address?
                if (address == firstPtr)
                {
                    cache[i] = cache[0];
                }
                else
                {
                    //Decode
                    cache[i] = Decode(address);
                }
            }

            return cache;
        }

        /// <summary>
        /// Decodes the Z string starting at the given address
        /// </summary>
        /// <param name="address">start address</param>
        /// <returns>a string containing the decoded string</returns>
        public string Decode(int address)
        {
            //Initialize decode state
            StringBuilder str = new StringBuilder();
            byte[] zChars = new byte[3];

            //Specials are
            // 0   = Not special
            // 1-3 = Abbreviations
            // 4   = ZSCII first char
            // 5   = ZSCII second char
            int special = 0;
            
            int alphabet = 0;
            int alphabetPerm = 0;
            bool alphabetKeep = false;

            int zsciiFirst = 0;

            //Start decode loop
            for (;;)
            {
                //Extract the 3 z characters
                ushort threeChar = buf.GetUShort(address);
                zChars[0] = (byte) ((threeChar >> 10) & 0x1F);
                zChars[1] = (byte) ((threeChar >> 5) & 0x1F);
                zChars[2] = (byte) (threeChar & 0x1F);

                //Process them
                for (int j = 0; j <= 2; j++)
                {
                    //Reset alphabet?
                    if (alphabetKeep)
                    {
                        alphabetKeep = false;
                    }
                    else
                    {
                        alphabet = alphabetPerm;
                    }

                    //Special multi character thingy
                    switch (special)
                    {
                        case 1:
                        case 2:
                        case 3:
                            //Abbreviation
                            if (abbreviationCache == null)
                            {
                                //Invalid character
                                throw new ZMachineException("z string contains abbreviation but abbreviations are not enabled");
                            }

                            str.Append(abbreviationCache[(special - 1) * 32 + zChars[j]]);
                            special = 0;
                            continue;

                        case 4:
                            //First ZSCII character
                            zsciiFirst = zChars[j];
                            special = 5;
                            continue;

                        case 5:
                            //Second ZSCII character
                            int zsciiChar = zChars[j] | (zsciiFirst << 5);

                            //Lookup character
                            if (zsciiChar < 0 || zsciiChar >= 256)
                            {
                                //Invalid
                                str.Append(INVALID_CHAR);
                            }
                            else
                            {
                                str.Append(unicodeCache[zsciiChar]);
                            }

                            special = 0;
                            continue;
                    }

                    //Handle special z characters
                    switch (zChars[j])
                    {
                        case 0:
                            //Space character
                            str.Append(' ');
                            break;

                        case 1:
                            //Abbreviation / Newline
                            if (machineVersion >= 2)
                            {
                                special = 1;
                            }
                            else
                            {
                                str.Append('\n');
                            }

                            break;

                        case 2:
                        case 3:
                            //Abbreviation / Temporary Shift
                            if (machineVersion >= 3)
                            {
                                special = zChars[j];
                            }
                            else
                            {
                                alphabet = (alphabetPerm + zChars[j] - 1) % 3;
                                alphabetKeep = true;
                            }

                            break;

                        case 4:
                        case 5:
                            //Shift Temp / Permenant
                            if (machineVersion >= 3)
                            {
                                //Shift temporary
                                alphabet = zChars[j] - 3;
                                alphabetKeep = true;
                            }
                            else
                            {
                                //Shift permenant
                                alphabetPerm = (alphabetPerm + zChars[j] - 3) % 3;
                            }

                            break;

                        case 6:
                            if (alphabet == 2)
                            {
                                //Start ZSCII character
                                special = 4;
                                break;
                            }

                            //Fallthrough to default
                            goto default;

                        default:
                            //Normal character
                            str.Append(alphabetCache[(alphabet * 26) + zChars[j] - 6]);
                            break;
                    }

                }

                //Exit now?
                if ((threeChar & 0x8000) != 0)
                {
                    break;
                }

                //Advance address
                address += 2;
            }

            //Return complete string
            return str.ToString();
        }
    }
}
