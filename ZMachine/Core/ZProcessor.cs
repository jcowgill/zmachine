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
        //Argument type constants
        private const int ArgumentLarge = 0;
        private const int ArgumentSmall = 1;
        private const int ArgumentVariable = 2;
        private const int ArgumentOmitted = 3;

        /// <summary>
        /// Maximum stack size in ushorts
        /// </summary>
        public const int MaxStackSize = 0x10000;

        /// <summary>
        /// Delegate called by the instructio processor to run an instruction
        /// </summary>
        /// <param name="argCount">The number of arguments passed</param>
        /// <param name="args">The argument data</param>
        /// <remarks>
        /// <para>Use argCount to test the number of arguments, not args.Length</para>
        /// </remarks>
        protected delegate void InstructionFunc(int argCount, ushort[] args);

        //Memory and instructions
        private readonly MemoryBuffer memBuf;
        private readonly int globalVarsOffset;
                //Pointer to add to global variable to find in memory (not pointer to the table itself)
        private readonly InstructionFunc[] instructions = new InstructionFunc[256];

        /*
         * Stack frame format
         * ---
         * 0 = Return frame pointer
         * 1 = Return program counter (little endian)
         * 3 = Lower 4 bits = number of local variables
         *     Next 4 bits = number of arguments
         *     Bit 8 = 1 if return program counter points to a store address to store result
         * 4+ = Local variables
         *     Frame evaluation stack
         */
        private ushort[] stack = new ushort[MaxStackSize];
        private int stackPtr;       //Pointer to next address to write to
        private int framePtr;       //Pointer to base of current stack frame

        //Used to lock execute to 1 call at once (1 = call in progress)
        private int executeLock = 0;

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
            //Store memory buffer
            memBuf = buf;

            //Cache global vars offset
            globalVarsOffset = buf.GetUShort(0xC) - 0x10;
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
                    return Memory.GetByte(ProgramCounter++);

                case ArgumentVariable:
                    //Get variable number
                    int varNum = Memory.GetByte(ProgramCounter++);

                    //Do get
                    if (varNum == 0)
                    {
                        //Pop from stack
                        if (stackPtr == framePtr + 4 + (stack[framePtr + 3] & 0xF))
                        {
                            throw new ZMachineException("stack underflow");
                        }

                        return stack[stackPtr--];
                    }
                    else if (varNum < 0x10)
                    {
                        //Get from local variable
                        if ((stack[framePtr + 3] & 0xF) < varNum)
                        {
                            throw new ZMachineException("attempted get from non-existant local variable");
                        }

                        return stack[framePtr + 3 + varNum];
                    }
                    else
                    {
                        //Get from global variable
                        return Memory.GetUShort(globalVarsOffset + varNum);
                    }

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
        /// Used to store the value in the "result" part of the instruction
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>
        /// <para>If this function is used by an instruction, it MUST ALWAYS be used.</para>
        /// <para>If InstructionStore and InstructionBranch are both used, InstructionStore must come first.</para>
        /// </remarks>
        protected void InstructionStore(ushort value)
        {
            //Get variable number
            int varNum = Memory.GetByte(ProgramCounter++);

            //Do store
            if (varNum == 0)
            {
                //Push on stack
                if (stackPtr >= MaxStackSize)
                {
                    throw new ZMachineException("stack overflow");
                }

                stack[stackPtr++] = value;
            }
            else if (varNum < 0x10)
            {
                //Store in local variable
                if ((stack[framePtr + 3] & 0xF) < varNum)
                {
                    throw new ZMachineException("attempted store to non-existant local variable");
                }

                stack[framePtr + 3 + varNum] = value;
            }
            else
            {
                //Store in global variable
                Memory.SetUShort(globalVarsOffset + varNum, value);
            }
        }

        /// <summary>
        /// Branches to another address depending on the result of an operation (passed as flag)
        /// </summary>
        /// <param name="result">result of the operation deciding the branch</param>
        protected void InstructionBranch(bool result)
        {
            //Read branch info and offset
            byte info = Memory.GetByte(ProgramCounter++);
            int offset;

            if ((info & 0x40) != 0)
            {
                //Single byte branch
                offset = info & 0x3F;
            }
            else
            {
                //Double byte branch
                offset = (info & 0x3F) << 8 | Memory.GetByte(ProgramCounter++);

                //Make signed
                if (offset >= (1 << 13))
                {
                    offset = (1 << 13) - offset;
                }
            }

            //Do branch
            if (((info & 0x80) != 0) == result)
            {
                //Handle special return branches
                if (offset == 0 || offset == 1)
                {
                    //Return value in offset
                    InstructionReturn((ushort) offset);
                }
                else
                {
                    //Move program counter
                    ProgramCounter += offset - 2;
                }
            }
        }

        /// <summary>
        /// Creates and transfers execution to a new stack frame
        /// </summary>
        /// <param name="address">the initial value of the program counter for the frame</param>
        /// <param name="locals">array containing initial values of the locals of this routine</param>
        /// <param name="argc">number of arguments being passed</param>
        /// <param name="storeResult">true to store the result of the call after it returns</param>
        protected void InstructionMakeFrame(int address, ushort[] locals, int args, bool storeResult)
        {
            //Validate parameters
            if (locals.Length > 15 || args > 15)
            {
                throw new ArgumentException("too many locals or arguments");
            }

            //Enough stack space?
            if (stackPtr + 4 + locals.Length > MaxStackSize)
            {
                throw new ZMachineException("stack overflow");
            }

            //Switch to new frame pointer
            stack[stackPtr] = (ushort) framePtr;
            framePtr = stackPtr;
            stackPtr++;

            //Construct routine info
            stack[stackPtr++] = (ushort) ProgramCounter;
            stack[stackPtr++] = (ushort) (ProgramCounter >> 16);
            stack[stackPtr++] = (ushort) (locals.Length | (args << 4) | (storeResult ? 0x100 : 0));

            //Copy locals
            Array.Copy(locals, 0, stack, stackPtr, locals.Length);
            stackPtr += locals.Length;

            //Set new program counter
            ProgramCounter = address;
        }

        /// <summary>
        /// Returns from the current stack frame with the given value
        /// </summary>
        /// <param name="retValue">return value of the function (may be discarded)</param>
        protected void InstructionReturn(ushort retValue)
        {
            //In first routine?
            if (framePtr == 0)
            {
                throw new ZMachineException("return from first routine");
            }

            //Saving result?
            bool saveResult = (stack[framePtr + 3] & 0x100) != 0;

            //Restore counter
            ProgramCounter = stack[framePtr + 1] | (stack[framePtr + 2] << 16);

            //Restore stack pointers
            stackPtr = framePtr;
            framePtr = stack[framePtr];

            //Save result
            if (saveResult)
            {
                InstructionStore(retValue);
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
