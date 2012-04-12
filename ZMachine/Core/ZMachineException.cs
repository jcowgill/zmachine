using System;
using System.Runtime.Serialization;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// An exception occured within the Z Machine (usually program fault)
    /// </summary>
    [Serializable]
    public class ZMachineException : Exception, ISerializable
    {
        public ZMachineException()
        {
        }

        public ZMachineException(string msg) : base(msg)
        {
        }

        public ZMachineException(string msg, Exception e) : base(msg, e)
        {
        }

        protected ZMachineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
