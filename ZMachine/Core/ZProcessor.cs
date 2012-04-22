using System;
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
        private readonly ZCharacterEncoder zEncoder;
        private readonly IZUserInterface zUi;
        private readonly int globalVarsOffset;
                //Pointer to add to global variable to find in memory (not pointer to the table itself)

        //Lists of instructions
        private readonly InstructionFunc[] instruction2OP = new InstructionFunc[0x20];
        private readonly InstructionFunc[] instruction1OP = new InstructionFunc[0x10];
        private readonly InstructionFunc[] instruction0OP = new InstructionFunc[0x10];
        private readonly InstructionFunc[] instructionVAR = new InstructionFunc[0x20];
        private readonly InstructionFunc[] instructionEXT = new InstructionFunc[256];

        /// <summary>
        /// Array containing 0OP instructions
        /// </summary>
        /// <remarks>
        /// These instructions will ALWAYS be passed 0 arguments
        /// </remarks>
        protected InstructionFunc[] Instructions0OP { get { return instruction0OP; } }

        /// <summary>
        /// Array containing 1OP instructions
        /// </summary>
        /// <remarks>
        /// These instructions will ALWAYS be passed 1 argument
        /// </remarks>
        protected InstructionFunc[] Instructions1OP { get { return instruction1OP; } }

        /// <summary>
        /// Array containing 2OP instructions
        /// </summary>
        /// <remarks>
        /// These instructions may pass 0 to 3 arguments
        /// </remarks>
        protected InstructionFunc[] Instructions2OP { get { return instruction2OP; } }

        /// <summary>
        /// Array containing variable instructions
        /// </summary>
        /// <remarks>
        /// These instructions may pass 0 to 3 arguments
        /// (except 0xC and 0x1A which may receive 7 arguments)
        /// </remarks>
        protected InstructionFunc[] InstructionsVAR { get { return instructionVAR; } }

        /// <summary>
        /// Array containing extended instructions
        /// </summary>
        /// <remarks>
        /// These instructions may pass 0 to 3 arguments
        /// </remarks>
        protected InstructionFunc[] InstructionsEXT { get { return instructionEXT; } }

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
        private int stackPtr;        //Pointer to next address to write to
        private int framePtr;        //Pointer to base of current stack frame
        private int framesCount = 1; //Number of frames on the stack

        //Used to lock execute to 1 call at once (1 = call in progress)
        private int executeLock = 0;

        /// <summary>
        /// Gets the memory buffer used by the processor
        /// </summary>
        public MemoryBuffer Memory { get { return memBuf; } }

        /// <summary>
        /// Returns the ZCharacterEncoder which can encode and decode text in this processor
        /// </summary>
        public ZCharacterEncoder TextEncoder { get { return zEncoder; } }

        /// <summary>
        /// Returns the IZUserInterface allowing interaction with the rest of the world
        /// </summary>
        public IZUserInterface UserInterface { get { return zUi; } }

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
        /// Creates a new z processor
        /// </summary>
        /// <param name="buf">memory buffer</param>
        /// <param name="ui">interface with the rest of the world</param>
        protected ZProcessor(MemoryBuffer buf, IZUserInterface ui)
        {
            //Validate params
            if (buf == null)
            {
                throw new ArgumentNullException("buf");
            }
            else if (ui == null)
            {
                throw new ArgumentNullException("ui");
            }

            //Cache global vars offset
            globalVarsOffset = buf.GetUShort(0xC) - 0x10;

            //Globals vars must be higher than the header (security)
            if (globalVarsOffset + 0x10 < 64)
            {
                throw new ZMachineException("global variables table must not reside in the header");
            }

            //Set dynamic limit
            buf.DynamicLimit = Math.Min((int) buf.GetUShort(0xE), 64);

            //Store memory buffer and ui
            memBuf = buf;
            zUi = ui;

            //Create encoder
            zEncoder = new ZCharacterEncoder(buf);
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
                    //Use InstructionGetVariable
                    return InstructionGetVariable(Memory.GetByte(ProgramCounter++));

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
            byte opNum;
            byte extendedOp = 0;
            InstructionFunc func;

            //Lock the function
            if(Interlocked.CompareExchange(ref executeLock, 1, 0) == 1)
            {
                throw new InvalidOperationException("execute cannot be called recursively or at the same time as another thread");
            }

            //Check max size
            if (Memory.Length > MaximumStorySize)
            {
                throw new ZMachineException("story is too large for this z machine");
            }

            //Do reset
            ProcessorReset();

            //Start decode loop
            while(!Finished)
            {
                //Get opcode of next instruction
                opNum = Memory.GetByte(ProgramCounter++);

                //Get arguments
                if(opNum < 0x80)
                {
                    //Long Instruction
                    args[0] = GetArgument((opNum & 0x40) == 0 ? ArgumentSmall : ArgumentVariable);
                    args[1] = GetArgument((opNum & 0x20) == 0 ? ArgumentSmall : ArgumentVariable);
                    argc = 2;

                    func = instruction2OP[opNum & 0x1F];
                }
                else if(opNum < 0xB0)
                {
                    //Short 1OP
                    args[0] = GetArgument((opNum >> 4) & 3);
                    argc = 1;

                    func = instruction1OP[opNum & 0xF];
                }
                else if(opNum < 0xC0)
                {
                    if (opNum == 0xBE)
                    {
                        //Extended
                        // Get function
                        extendedOp = Memory.GetByte(ProgramCounter++);
                        func = instructionEXT[extendedOp];

                        // Get variable args
                        byte argTypes = Memory.GetByte(ProgramCounter++);
                        argc = LoadMultipleArguments(argTypes, 0, args);
                    }
                    else
                    {
                        //Short 0OP
                        argc = 0;
                        func = instruction0OP[opNum & 0xF];
                    }
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

                    func = instructionVAR[opNum & 0x1F];
                }
                else
                {
                    //Variable instruction
                    byte argTypes = Memory.GetByte(ProgramCounter++);
                    argc = LoadMultipleArguments(argTypes, 0, args);

                    //Can be 2OP or VAR
                    if ((opNum & 0x20) == 0)
                    {
                        //2OP
                        func = instruction2OP[opNum & 0x1F];
                    }
                    else
                    {
                        //VAR
                        func = instructionVAR[opNum & 0x1F];
                    }
                }

                //Do call
                if(func == null)
                {
                    //Illegal instruction
                    string message;
                    if (opNum == 0xBE)
                    {
                        message = "illegal extended instruction 0x" + extendedOp.ToString("X2");
                    }
                    else
                    {
                        message = "illegal instruction 0x" + opNum.ToString("X2");
                    }

                    throw new ZMachineException(message);
                }
                else
                {
                    func(argc, args);
                }
            }
        }

        /// <summary>
        /// Gets the value if the given variable
        /// </summary>
        /// <param name="variable">variable number</param>
        /// <returns>the variable's value</returns>
        protected ushort InstructionGetVariable(ushort variable)
        {
            //Do get
            if (variable == 0)
            {
                //Pop from stack
                if (stackPtr == framePtr + 4 + (stack[framePtr + 3] & 0xF))
                {
                    throw new ZMachineException("stack underflow");
                }

                return stack[stackPtr--];
            }
            else if (variable < 0x10)
            {
                //Get from local variable
                if ((stack[framePtr + 3] & 0xF) < variable)
                {
                    throw new ZMachineException("attempted get from non-existant local variable");
                }

                return stack[framePtr + 3 + variable];
            }
            else if (variable < 0x100)
            {
                //Get from global variable
                return Memory.GetUShort(globalVarsOffset + variable);
            }
            else
            {
                //Illegal variable number
                throw new ZMachineException("invalid variable number 0x" + variable.ToString("X4"));
            }
        }

        /// <summary>
        /// Used to store a value in a variable (stack, local, or global)
        /// </summary>
        /// <param name="variable">variable number to store into</param>
        /// <param name="value">value to store</param>
        protected void InstructionStoreVariable(ushort variable, ushort value)
        {
            //Do store
            if (variable == 0)
            {
                //Push on stack
                if (stackPtr >= MaxStackSize)
                {
                    throw new ZMachineException("stack overflow");
                }

                stack[stackPtr++] = value;
            }
            else if (variable < 0x10)
            {
                //Store in local variable
                if ((stack[framePtr + 3] & 0xF) < variable)
                {
                    throw new ZMachineException("attempted store to non-existant local variable");
                }

                stack[framePtr + 3 + variable] = value;
            }
            else if (variable < 0x100)
            {
                //Store in global variable
                Memory.SetUShort(globalVarsOffset + variable, value);
            }
            else
            {
                //Illegal variable number
                throw new ZMachineException("invalid variable number 0x" + variable.ToString("X4"));
            }
        }

        /// <summary>
        /// Used to store the value in the "result" part of the instruction
        /// </summary>
        /// <param name="value">value to store</param>
        /// <remarks>
        /// <para>This instruction is a post-argument method.</para>
        /// <para>If multiple post-argument methods are used, InstructionStore must always be first.</para>
        /// </remarks>
        protected void InstructionStore(ushort value)
        {
            //Pass to proper storer
            InstructionStoreVariable(Memory.GetByte(ProgramCounter++), value);
        }

        /// <summary>
        /// Branches to another address depending on the result of an operation (passed as flag)
        /// </summary>
        /// <param name="result">result of the operation deciding the branch</param>
        /// <remarks>
        /// <para>This instruction is a post-argument method.</para>
        /// <para>If multiple post-argument methods are used, InstructionBranch must always be last.</para>
        /// <para>This method cannot be used with InstructionGetString.</para>
        /// </remarks>
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
        /// Gets the string stored at the end of this instruction
        /// </summary>
        /// <returns>the string to process</returns>
        /// <remarks>
        /// <para>This instruction is a post-argument method.</para>
        /// <para>If multiple post-argument methods are used, InstructionGetString must always be last.</para>
        /// <para>This method cannot be used with InstructionBranch.</para>
        /// </remarks>
        protected string InstructionGetString()
        {
            //Decode string
            DecodeResult result = zEncoder.DecodeWithEnd(ProgramCounter);

            //Update counter and return result
            ProgramCounter = result.EndAddress;
            return result.Result;
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
            framesCount++;

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
            framesCount--;

            //Save result
            if (saveResult)
            {
                InstructionStore(retValue);
            }
        }

        /// <summary>
        /// Creates a snapshot of the execution state
        /// </summary>
        /// <returns>a snaphot of the current state</returns>
        public ZSnapshot CreateSnapshot()
        {
            return new ZSnapshot(Memory, stack, framePtr, stackPtr, framesCount);
        }

        /// <summary>
        /// Restores a snapshot of the execution state
        /// </summary>
        /// <param name="snapshot">snapshot to restore</param>
        public void RestoreSnapshot(ZSnapshot snapshot)
        {
            //Validate snapshot
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }
            else if (snapshot.DynamicLimit != Memory.DynamicLimit)
            {
                throw new ArgumentException("snapshot memoy size does not equal the memory size of the processor");
            }
            
            //Restore each part
            snapshot.RestoreStackData(stack);
            snapshot.RestoreMemory(Memory);
            framePtr = snapshot.FramePointer;
            framesCount = snapshot.FramesCount;
            stackPtr = snapshot.StackPointer;
        }

        /// <summary>
        /// Gets the maximum story size supported by the z machine
        /// </summary>
        public abstract int MaximumStorySize { get; }

        /// <summary>
        /// Called at the beginning of Execute and when restoring the game
        /// to reset header entries in the processor
        /// </summary>
        protected abstract void ProcessorReset();

        /// <summary>
        /// Translates the routine packed address provided into a byte address
        /// </summary>
        /// <param name="packedAddr">packed address to convert</param>
        protected abstract int TranslateRoutineAddress(ushort packedAddr);

        /// <summary>
        /// Translates the packed string address provided into a byte address
        /// </summary>
        /// <param name="packedAddr">packed address to convert</param>
        protected abstract int TranslatePackedStrAddress(ushort packedAddr);
    }
}
