using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// A generic z-machine processor
    /// </summary>
    /// <remarks>
    /// This class contains common features of the z processor which apply to every version.
    /// </remarks>
    public abstract class ZProcessor
    {
        private readonly MemoryBuffer memBuf;
        private readonly InstructionFunc[] instructions = new InstructionFunc[256];

        //Used to lock execute to 1 call at once (1 = call in progress)
        private int executeLock = 0;

        //Argument type constants
        private const int ArgumentLarge = 0;
        private const int ArgumentSmall = 1;
        private const int ArgumentVariable = 2;
        private const int ArgumentOmitted = 3;

        /// <summary>
        /// Delegate called by the instructio processor to run an instruction
        /// </summary>
        /// <param name="argCount">The number of arguments passed</param>
        /// <param name="args">The argument data</param>
        /// <remarks>
        /// <para>Use argCount to test the number of arguments, not args.Length</para>
        /// </remarks>
        protected delegate void InstructionFunc(int argCount, ushort[] args);

        /// <summary>
        /// Gets the memory buffer used by the processor
        /// </summary>
        public MemoryBuffer Memory { get { return memBuf; } }

        /// <summary>
        /// Gets or sets the location of the next instruction to execute
        /// </summary>
        public int ProgramCounter { get; protected set; }

        /// <summary>
        /// Marks the program as complete
        /// </summary>
        /// <remarks>
        /// When set to true, the Execute method will return after the instruction has returned
        /// </remarks>
        public bool Finished { get; protected set; }

        /// <summary>
        /// Creates a z processor with the given memory buffer
        /// </summary>
        /// <param name="buf">memory buffer</param>
        public ZProcessor(MemoryBuffer buf)
        {
            memBuf = buf;
        }

        /// <summary>
        /// Gets the instruction with the given number
        /// </summary>
        /// <param name="number">instruction number</param>
        protected InstructionFunc GetInstruction(int number)
        {
            if (number < 0 || number > 256)
            {
                throw new ArgumentOutOfRangeException("number");
            }

            return instructions[number];
        }

        /// <summary>
        /// Sets the instruction function with the given number
        /// </summary>
        /// <param name="number">instruction number</param>
        /// <param name="op">instruction function</param>
        /// <remarks>
        /// The instruction number affects how the instruction format is interpreted.
        /// </remarks>
        protected void SetInstruction(int number, InstructionFunc op)
        {
            if (number < 0 || number > 256)
            {
                throw new ArgumentOutOfRangeException("number");
            }

            instructions[number] = op;
        }

        /// <summary>
        /// Gets an argument for an instruction from memory
        /// </summary>
        /// <param name="type">argument type</param>
        private ushort GetArgument(int type)
        {
            switch(type)
            {
                case ArgumentLarge:
                    //UShort
                    ushort value = Memory.GetUShort(ProgramCounter);
                    ProgramCounter += 2;
                    return value;

                case ArgumentSmall:
                    //Byte
                    value = Memory.GetByte(ProgramCounter++);
                    return value;

                case ArgumentVariable:
                    //Get from stack, locals or globals
                    //TODO
                    return 0;

                case ArgumentOmitted:
                default:
                    //Should not happen
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        /// <summary>
        /// Loads up to 4 arguments into the args array according to the types in argTypes
        /// </summary>
        /// <param name="argTypes">byte specifying the types of argument</param>
        /// <param name="startPtr">position to start loading bytes at</param>
        /// <param name="args">array to place arguments</param>
        /// <return>number of arguments loaded</return>
        private int LoadMultipleArguments(byte argTypes, int startPtr, ushort[] args)
        {
            for(int i = 0; i < 4; i++)
            {
                //Argument omitted?
                if(argTypes >> 6 == ArgumentOmitted)
                {
                    return i;
                }

                //Get argument
                args[startPtr++] = GetArgument(argTypes >> 6);

                //Shift left 1 arg
                argTypes <<= 2;
            }

            //All arguments given
            return 4;
        }

        /// <summary>
        /// Starts executing at the address in the program counter
        /// </summary>
        /// <exception cref="ZMachineException">Thrown when there is an error executing an instruction.</exception>
        public void Execute()
        {
            ushort[] args = new ushort[8];
            int argc;

            //Lock the function
            if(Interlocked.CompareExchange(ref executeLock, 1, 0) == 1)
            {
                throw new InvalidOperationException("execute cannot be called recursively or at the same time as another thread");
            }

            try
            {
                //Start decode loop
                while(!Finished)
                {
                    //Get opcode of next instruction
                    byte opNum = Memory.GetByte(ProgramCounter++);

                    //Get arguments
                    if(opNum < 0x80)
                    {
                        //Long Instruction
                        args[0] = GetArgument((opNum & 0x40) == 0 ? ArgumentSmall : ArgumentVariable);
                        args[1] = GetArgument((opNum & 0x20) == 0 ? ArgumentSmall : ArgumentVariable);
                        argc = 2;
                    }
                    else if(opNum < 0xB0)
                    {
                        //Short 1OP
                        args[0] = GetArgument((opNum >> 4) & 3);
                        argc = 1;
                    }
                    else if(opNum < 0xC0)
                    {
                        //Short 0OP / Extended (0xBE)
                        argc = 0;
                    }
                    else if(opNum == 0xEC || opNum == 0xFA)
                    {
                        //Variable call_vs2 and call_vn2 use 8 arguments
                        byte argTypes1 = Memory.GetByte(ProgramCounter++);
                        byte argTypes2 = Memory.GetByte(ProgramCounter++);

                        argc = LoadMultipleArguments(argTypes1, 0, args);

                        if(argc == 4)
                        {
                            argc = 4 +  LoadMultipleArguments(argTypes2, 4, args);
                        }
                    }
                    else
                    {
                        //Variable instruction
                        byte argTypes = Memory.GetByte(ProgramCounter++);
                        argc = LoadMultipleArguments(argTypes, 0, args);
                    }

                    //Do call
                    if(instructions[opNum] == null)
                    {
                        //Illegal instruction
                        throw new ZMachineException("illegal instruction 0x" + opNum.ToString("X2"));
                    }
                    else
                    {
                        instructions[opNum](argc, args);
                    }
                }
            }
            finally
            {
                //Unlock
                executeLock = 0;
            }
        }

        /// <summary>
        /// Translates the routine packed address provided into a byte address
        /// </summary>
        /// <param name="packedAddr">packed address to convert</param>
        protected abstract int TranslateRoutineAddress(ushort packedAddr);

        /// <summary>
        /// Translates the packed string address provided into a byte address
        /// </summary>
        /// <param name="packedAddr">packed address to convert</param>
        protected virtual int TranslatePackedStrAddress(ushort packedAddr)
        {
            return TranslateRoutineAddress(packedAddr);
        }
    }
}
