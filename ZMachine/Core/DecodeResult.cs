namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// The result of decoding a string using DecodeWithEnd
    /// </summary>
    public struct DecodeResult
    {
        /// <summary>
        /// The string containing the decoded result
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// The address after that of the last character in the string
        /// </summary>
        public int EndAddress { get; set; }

        public override bool Equals(object obj)
        {
            //Attempt to unbox object
            if (obj is DecodeResult)
            {
                DecodeResult dResult = (DecodeResult) obj;

                //Test equality
                return Result.Equals(dResult.Result) && EndAddress == dResult.EndAddress;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return 37 * (17 * 37 + (int) EndAddress) + Result.GetHashCode();
        }

        public static bool operator ==(DecodeResult a, DecodeResult b)
        {
            return a.Result.Equals(b.Result) && a.EndAddress == b.EndAddress;
        }

        public static bool operator !=(DecodeResult a, DecodeResult b)
        {
            return !a.Result.Equals(b.Result) || a.EndAddress != b.EndAddress;
        }
    }
}
