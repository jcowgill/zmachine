using System;
using JCowgill.ZMachine.Core;

namespace JCowgill.ZMachine.Machine
{
    /// <summary>
    /// The Z Machine Version 1
    /// </summary>
    /// <remarks>
    /// Version 1 contains the bulk of the "basic" instructions so other machine
    /// versions will extend from this one.
    /// </remarks>
    public class MachineV1 : ZProcessor
    {
        private Random rndGenerator = new Random();

        #region Instructions

        /// <summary>
        /// Creates a new version 1 z processor
        /// </summary>
        /// <param name="buf">memory buffer</param>
        /// <param name="ui">interface with the rest of the world</param>
        public MachineV1(MemoryBuffer buf, IUserInterface ui)
            : base(buf, ui)
        {
            InstructionFunc[] ops;

            //2OP instructions
            ops = Instructions2OP;
            ops[0x1] = Je;
            ops[0x2] = Jl;
            ops[0x3] = Jg;
            ops[0x4] = DecChk;
            ops[0x5] = IncChk;
            ops[0x6] = Jin;
            ops[0x7] = Test;
            ops[0x8] = Or;
            ops[0x9] = And;
            ops[0xA] = TestAttr;
            ops[0xB] = SetAttr;
            ops[0xC] = ClearAttr;
            ops[0xD] = Store;
            ops[0xE] = InsertObj;
            ops[0xF] = LoadW;
            ops[0x10] = LoadB;
            ops[0x11] = GetProp;
            ops[0x12] = GetPropAddr;
            ops[0x13] = GetNextProp;
            ops[0x14] = Add;
            ops[0x15] = Sub;
            ops[0x16] = Mul;
            ops[0x17] = Div;
            ops[0x18] = Mod;

            //1OP Instructions
            ops = Instructions1OP;
            ops[0x0] = Jz;
            ops[0x1] = GetSibling;
            ops[0x2] = GetChild;
            ops[0x3] = GetParent;
            ops[0x4] = GetPropLen;
            ops[0x5] = Inc;
            ops[0x6] = Dec;
            ops[0x7] = PrintAddr;

            ops[0x9] = RemoveObj;
            ops[0xA] = PrintObj;
            ops[0xB] = Ret;
            ops[0xC] = Jump;
            ops[0xD] = PrintPAddr;
            ops[0xE] = Load;
            ops[0xF] = Not;

            //0OP Instructions
            ops = Instructions0OP;
            ops[0x0] = RTrue;
            ops[0x1] = RFalse;
            ops[0x2] = Print;
            ops[0x3] = PrintRet;
            ops[0x4] = Nop;
            ops[0x5] = Save;
            ops[0x6] = Restore;
            ops[0x7] = Restart;
            ops[0x8] = RetPopped;
            ops[0x9] = Pop;
            ops[0xA] = Quit;
            ops[0xB] = NewLine;

            //VAR Instructions
            ops = InstructionsVAR;
            ops[0x0] = GenericCall;
            ops[0x0] = StoreW;
            ops[0x0] = StoreB;
            ops[0x0] = PutProp;
            ops[0x0] = Read;
            ops[0x0] = PrintChar;
            ops[0x0] = PrintNum;
            ops[0x0] = RandomFunc;
            ops[0x0] = Push;
            ops[0x0] = Pull;
        }

        private void Je(int argc, ushort[] argv)
        {
            //Jump if equal (0 is equal to any of the others)
            // Must have >= 2 operands
            if(argc < 2)
                throw new ZMachineException("je requires at least 2 operands");

            // Get argument
            ushort arg0 = argv[0];

            // Do branch
            InstructionBranch(
                    arg0 == argv[1] ||
                    (argc >= 3 && (arg0 == argv[2] ||
                    (argc >= 4 && arg0 == argv[3])))
                );
        }

        private void Jl(int argc, ushort[] argv)
        {
            //Jump if a < b
            if (argc != 2)
                throw new ZMachineException("jl requires 2 operands");

            InstructionBranch((short) argv[0] < (short) argv[1]);
        }

        private void Jg(int argc, ushort[] argv)
        {
            //Jump if a > b
            if (argc != 2)
                throw new ZMachineException("jl requires 2 operands");

            InstructionBranch((short) argv[0] > (short) argv[1]);
        }

        private void Dec(int argc, ushort[] argv)
        {
            //Decrement and store
            short value = (short) (InstructionGetVariable(argv[0]) - 1);
            InstructionStoreVariable(argv[0], (ushort) value);
        }

        private void Inc(int argc, ushort[] argv)
        {
            //Increment and store
            short value = (short) (InstructionGetVariable(argv[0]) + 1);
            InstructionStoreVariable(argv[0], (ushort) value);
        }

        private void DecChk(int argc, ushort[] argv)
        {
            //Decrement variable and jump if less than supplied value
            if (argc != 2)
                throw new ZMachineException("dec_chk requires 2 operands");

            //Decrement and store
            short value = (short) (InstructionGetVariable(argv[0]) - 1);
            InstructionStoreVariable(argv[0], (ushort) value);

            //Branch?
            InstructionBranch(value < (short) argv[1]);
        }

        private void IncChk(int argc, ushort[] argv)
        {
            //Increment variable and jump if greater than supplied value
            if (argc != 2)
                throw new ZMachineException("inc_chk requires 2 operands");

            //Increment and store
            short value = (short) (InstructionGetVariable(argv[0]) + 1);
            InstructionStoreVariable(argv[0], (ushort) value);

            //Branch?
            InstructionBranch(value > (short) argv[1]);
        }

        private void Jin(int argc, ushort[] argv)
        {
            //Jump if parent of a is b
            if (argc != 2)
                throw new ZMachineException("jin requires 2 operands");

            InstructionBranch(ObjectTree.GetParent(argv[0]) == argv[1]);
        }

        private void Test(int argc, ushort[] argv)
        {
            //Test if all flags in bitmap are set
            if (argc != 2)
                throw new ZMachineException("test requires 2 operands");

            InstructionBranch((argv[0] & argv[1]) == argv[1]);
        }

        private void Or(int argc, ushort[] argv)
        {
            //Bitwise or
            if (argc != 2)
                throw new ZMachineException("or requires 2 operands");

            InstructionStore((ushort) (argv[0] | argv[1]));
        }

        private void And(int argc, ushort[] argv)
        {
            //Bitwise and
            if (argc != 2)
                throw new ZMachineException("and requires 2 operands");

            InstructionStore((ushort) (argv[0] & argv[1]));
        }

        private void TestAttr(int argc, ushort[] argv)
        {
            //Jump if object has the specified attribute
            if (argc != 2)
                throw new ZMachineException("test_attr requires 2 operands");

            InstructionBranch(ObjectTree.GetAttribute(argv[0], argv[1]));
        }

        private void SetAttr(int argc, ushort[] argv)
        {
            //Give object an attribute
            if (argc != 2)
                throw new ZMachineException("set_attr requires 2 operands");

            ObjectTree.SetAttribute(argv[0], argv[1], true);
        }

        private void ClearAttr(int argc, ushort[] argv)
        {
            //Clear attribute of an object
            if (argc != 2)
                throw new ZMachineException("clear_attr requires 2 operands");

            ObjectTree.SetAttribute(argv[0], argv[1], false);
        }

        private void Store(int argc, ushort[] argv)
        {
            //Store value in a variable
            if (argc != 2)
                throw new ZMachineException("store requires 2 operands");

            //Do store
            InstructionStoreVariable(argv[0], argv[1]);
        }

        private void InsertObj(int argc, ushort[] argv)
        {
            //Make move a to become a child of b
            if (argc != 2)
                throw new ZMachineException("insert_obj requires 2 operands");

            //Check insert to object 0
            if (argv[1] == 0)
                throw new ZMachineException("destination object is 0 in insert_obj");

            ObjectTree.SetParent(argv[0], argv[1]);
        }

        private void LoadW(int argc, ushort[] argv)
        {
            //Loads a word in an array and stores it
            if (argc != 2)
                throw new ZMachineException("loadw requires 2 operands");

            InstructionStore(Memory.GetUShort(argv[0] + 2 * argv[1]));
        }

        private void LoadB(int argc, ushort[] argv)
        {
            //Loads a byte in an array and stores it
            if (argc != 2)
                throw new ZMachineException("loadb requires 2 operands");

            InstructionStore(Memory.GetByte(argv[0] + argv[1]));
        }

        private void GetProp(int argc, ushort[] argv)
        {
            //Reads a property from an object and stores it
            if (argc != 2)
                throw new ZMachineException("get_prop requires 2 operands");

            //Get property address
            int address = ObjectTree.GetPropertyAddress(argv[0], argv[1]);
            ushort value;

            //Exists?
            if (address == 0)
            {
                //Get default value
                value = ObjectTree.GetDefaultProperty(argv[1]);
            }
            else
            {
                //Get property value
                int length = ObjectTree.GetPropertyLength(address);

                switch (length)
                {
                    case 1:
                        value = Memory.GetByte(address);
                        break;

                    case 2:
                        value = Memory.GetUShort(address);
                        break;

                    default:
                        throw new ZMachineException("properties accessed using get_prop must be 1 or 2 bytes long");
                }
            }

            //Store value
            InstructionStore(value);
        }

        private void GetPropAddr(int argc, ushort[] argv)
        {
            //Reads the address of the data stored in a property
            if (argc != 2)
                throw new ZMachineException("get_prop_addr requires 2 operands");

            InstructionStore((ushort) ObjectTree.GetPropertyAddress(argv[0], argv[1]));
        }

        private void GetNextProp(int argc, ushort[] argv)
        {
            //Reads the number of the next property an object has
            if (argc != 2)
                throw new ZMachineException("get_next_prop requires 2 operands");

            InstructionStore((ushort) ObjectTree.GetNextProperty(argv[0], argv[1]));
        }

        private void Add(int argc, ushort[] argv)
        {
            //Signed addition
            if (argc != 2)
                throw new ZMachineException("add requires 2 operands");

            InstructionStore((ushort) ((short) argv[0] + (short) argv[1]));
        }

        private void Sub(int argc, ushort[] argv)
        {
            //Signed subtraction
            if (argc != 2)
                throw new ZMachineException("sub requires 2 operands");

            InstructionStore((ushort) ((short) argv[0] - (short) argv[1]));
        }

        private void Mul(int argc, ushort[] argv)
        {
            //Signed multiplication
            if (argc != 2)
                throw new ZMachineException("mul requires 2 operands");

            InstructionStore((ushort) ((short) argv[0] * (short) argv[1]));
        }

        private void Div(int argc, ushort[] argv)
        {
            //Signed division
            if (argc != 2)
                throw new ZMachineException("div requires 2 operands");

            //Check for division by 0
            if (argv[1] == 0)
                throw new ZMachineException("division by 0");

            InstructionStore((ushort) ((short) argv[0] / (short) argv[1]));
        }

        private void Mod(int argc, ushort[] argv)
        {
            //Remainder of division
            if (argc != 2)
                throw new ZMachineException("mod requires 2 operands");

            //Check for division by 0
            if (argv[1] == 0)
                throw new ZMachineException("division by 0");

            InstructionStore((ushort) ((short) argv[0] % (short) argv[1]));
        }

        private void Jz(int argc, ushort[] argv)
        {
            //Jump if 0
            InstructionBranch(argv[0] == 0);
        }

        private void GetSibling(int argc, ushort[] argv)
        {
            //Gets the next object in the tree
            int sibling = ObjectTree.GetSibling(argv[0]);
            InstructionStore((ushort) sibling);
            InstructionBranch(sibling != 0);
        }

        private void GetChild(int argc, ushort[] argv)
        {
            //Gets the first child of an object in the tree
            int child = ObjectTree.GetChild(argv[0]);
            InstructionStore((ushort) child);
            InstructionBranch(child != 0);
        }

        private void GetParent(int argc, ushort[] argv)
        {
            //Gets the parent of an object in the tree
            InstructionStore((ushort) ObjectTree.GetParent(argv[0]));
        }

        private void GetPropLen(int argc, ushort[] argv)
        {
            //Gets the length of the given property (by address)
            InstructionStore((ushort) ObjectTree.GetPropertyLength(argv[0]));
        }

        private void PrintAddr(int argc, ushort[] argv)
        {
            //Prints the z encoded string at the given address
            string str = TextEncoder.Decode(argv[0]);

            //TODO print_addr
        }

        private void PrintPAddr(int argc, ushort[] argv)
        {
            //Prints the packed z encoded string at the given address
            string str = TextEncoder.Decode(TranslatePackedStrAddress(argv[0]));

            //TODO print_addr
        }

        private void RemoveObj(int argc, ushort[] argv)
        {
            //Detaches the object from its parent
            ObjectTree.SetParent(argv[0], 0);
        }

        private void PrintObj(int argc, ushort[] argv)
        {
            //Prints the short name of an object
            string str = TextEncoder.Decode(ObjectTree.GetObjectName(argv[0]));

            //TODO print_obj
        }

        private void Ret(int argc, ushort[] argv)
        {
            //Returns from a routine
            InstructionReturn(argv[0]);
        }

        private void Jump(int argc, ushort[] argv)
        {
            //Unconditional relative jump
            ProgramCounter += (short) argv[0] - 2;
        }

        private void Load(int argc, ushort[] argv)
        {
            //Store the value of a variable
            InstructionStore(InstructionGetVariable(argv[0]));
        }

        private void Not(int argc, ushort[] argv)
        {
            //Bitwise not
            // This is validated as it is moved to EXT in version 5
            if (argc != 1)
                throw new ZMachineException("not requires 1 operand");

            //Bitwise NOT
            InstructionStore((ushort) ~argv[0]);
        }

        private void RTrue(int argc, ushort[] argv)
        {
            //Return true
            InstructionReturn(1);
        }

        private void RFalse(int argc, ushort[] argv)
        {
            //Return false
            InstructionReturn(0);
        }

        private void Print(int argc, ushort[] argv)
        {
            //Print inline string
            string str = InstructionGetString();

            //TODO print
        }

        private void PrintRet(int argc, ushort[] argv)
        {
            //Print inline string, newline and return true
            string str = InstructionGetString();

            //TODO print_ret

            //Return
            InstructionReturn(1);
        }

        private void Nop(int argc, ushort[] argv)
        {
            //No operation
        }

        private void Save(int argc, ushort[] argv)
        {
            //Save and branch on success
            //TODO save
        }

        private void Restore(int argc, ushort[] argv)
        {
            //Restore (branch exists but is ignored)
            //TODO save
        }

        private void Restart(int argc, ushort[] argv)
        {
            //Restarts the game
            //TODO restart
        }

        private void RetPopped(int argc, ushort[] argv)
        {
            //Returns value on stack
            InstructionReturn(InstructionGetVariable(0));
        }

        private void Pop(int argc, ushort[] argv)
        {
            //Pop value and discard
            InstructionGetVariable(0);
        }

        private void Quit(int argc, ushort[] argv)
        {
            //Exit immediately
            Finished = true;
        }

        private void NewLine(int argc, ushort[] argv)
        {
            //Print new line
            //TODO new_line
        }

        private void GenericCallImpl(int argc, ushort[] argv, bool storeResult)
        {
            //Implements the GenericCall and GenericCallAndDiscard methods

            //Get call address
            if (argc == 0)
                throw new ZMachineException("all call instructions require at least 1 operand");

            int address = TranslateRoutineAddress(argv[0]);

            //If 0, return 0
            if (address == 0)
            {
                InstructionStore(0);
            }
            else
            {
                //Get number of locals and arguments
                byte localCount = Memory.GetByte(address++);
                int routineArgs = Math.Min(localCount, argc - 1);

                if (localCount > 15)
                    throw new ZMachineException("routine at 0x" + address.ToString("X8") + " contains more than 15 locals");

                //Initialize locals list
                ushort[] locals = new ushort[localCount];

                // Arguments
                for (int i = 0; i < routineArgs; i++)
                {
                    locals[i] = argv[i + 1];
                }

                // Extra initializers
                if (InitializeRoutineLocals)
                {
                    //Use initializers after locals count
                    for (int i = routineArgs; i < localCount; i++)
                    {
                        locals[i] = Memory.GetUShort(address + i * 2);
                    }

                    //Advance address to start of routine
                    address += localCount * 2;
                }

                //Create stack frame
                InstructionMakeFrame(address, locals, routineArgs, storeResult);
            }
        }

        /// <summary>
        /// Generic call which stores the result and works with any number of arguments
        /// </summary>
        protected void GenericCall(int argc, ushort[] argv)
        {
            GenericCallImpl(argc, argv, true);
        }

        /// <summary>
        /// Generic call which discards the result and works with any number of arguments
        /// </summary>
        protected void GenericCallAndDiscard(int argc, ushort[] argv)
        {
            GenericCallImpl(argc, argv, false);
        }

        /// <summary>
        /// Call this when the "story" sets a byte - does lots of checks
        /// </summary>
        /// <param name="address">address to set</param>
        /// <param name="data">value to store</param>
        protected void StorySetByte(int address, byte data)
        {
            //Check for stores in the header
            if (address < 64)
            {
                //It is valid to set flags 2 however
                if (address == 0x10)
                {
                    //TODO restrict and set flags 2
                    return;
                }

                throw new ZMachineException("illegal \"set\" for memory address 0x" + address.ToString("X8"));
            }

            //Do set
            Memory.SetByte(address, data);
        }

        /// <summary>
        /// Call this when the "story" sets a ushort - does lots of checks
        /// </summary>
        /// <param name="address">address to set</param>
        /// <param name="data">value to store</param>
        protected void StorySetUShort(int addrees, ushort data)
        {
            StorySetByte(addrees, (byte) (data >> 8));
            StorySetByte(addrees + 1, (byte) data);
        }

        private void StoreB(int argc, ushort[] argv)
        {
            //Store byte at index in specified table
            if (argc != 3)
                throw new ZMachineException("storeb requires 3 operands");

            StorySetByte(argv[0] + argv[1], (byte) argv[2]);
        }

        private void StoreW(int argc, ushort[] argv)
        {
            //Store ushort at index in specified table
            if (argc != 3)
                throw new ZMachineException("storew requires 3 operands");

            StorySetUShort(argv[0] + 2 * argv[1], argv[2]);
        }

        private void PutProp(int argc, ushort[] argv)
        {
            //Changes the value of the property
            if (argc != 3)
                throw new ZMachineException("put_prop requires 3 operands");

            ObjectTree.PutProperty(argv[0], argv[1], argv[2]);
        }

        private void Read(int argc, ushort[] argv)
        {
            //Reads a command
            if (argc != 2)
                throw new ZMachineException("read requires 2 operands");

            //TODO read
        }

        private void PrintChar(int argc, ushort[] argv)
        {
            //Prints a ZSCII character
            if (argc != 1)
                throw new ZMachineException("print_char requires 1 operand");

            //Get character
            char c = TextEncoder.ZsciiToUnicode(argv[0]);

            //TODO print_char
        }

        private void PrintNum(int argc, ushort[] argv)
        {
            //Prints a signed number
            if (argc != 1)
                throw new ZMachineException("print_num requires 1 operand");

            //Get numeric representation
            string str = ((short) argv[0]).ToString();

            //TODO print_char
        }

        private void RandomFunc(int argc, ushort[] argv)
        {
            ushort retVal = 0;

            //Gets a random number or reseed the generator
            if (argc != 1)
                throw new ZMachineException("random requires 1 operand");

            //What to do?
            if ((short) argv[0] > 0)
            {
                //Get random number from 1 to a
                retVal = (ushort) rndGenerator.Next(1, argv[0] + 1);
            }
            else if (argv[0] == 0)
            {
                //Seed with random value
                rndGenerator = new Random();
            }
            else
            {
                //Seed with predictable value
                rndGenerator = new Random((short) argv[0]);
            }

            //Store result
            InstructionStore(retVal);
        }

        private void Push(int argc, ushort[] argv)
        {
            //Push a value (directly) onto the stack
            if (argc != 1)
                throw new ZMachineException("push requires 1 operand");

            //Do store to var 0
            InstructionStoreVariable(0, argv[0]);
        }

        private void Pull(int argc, ushort[] argv)
        {
            //Pop variable from the stack
            if (argc != 1)
                throw new ZMachineException("pull requires 1 operand");

            //Ignore if restoring onto the stack
            if (argv[0] != 0)
            {
                InstructionStoreVariable(argv[0], InstructionGetVariable(0));
            }
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// The maximum size of a story in V1-3 (128 KB)
        /// </summary>
        public const int MaximumStorySizeV1 = 128 * 1024;

        public override int MaximumStorySize { get { return MaximumStorySizeV1; } }

        protected override void ProcessorReset()
        {
            //Reset V1 fields in the header
            //TODO fix all this

            // Flags 1
            byte flags1 = Memory.GetByte(0x1);
            flags1 |= (1 << 4);                         //No status line
            flags1 &= ~((1 << 5) | (1 << 6)) & 0xFF;    //No screen splitting, variable-pitch is not default
            Memory.SetByte(0x1, flags1);

            // Flags 2
            byte flags2 = Memory.GetByte(0x10);
            flags2 &= ~1 & 0xFF;                        //No transcription
            Memory.SetByte(0x10, flags2);

            // Standards number
            Memory.SetByte(0x32, 0);        //Major revision
            Memory.SetByte(0x33, 0);        //Minor revision
        }

        protected override int TranslateRoutineAddress(ushort packedAddr)
        {
            //2x packed
            return 2 * packedAddr;
        }

        protected override int TranslatePackedStrAddress(ushort packedAddr)
        {
            //2x packed
            return 2 * packedAddr;
        }

        /// <summary>
        /// Property is true if routines initialize their local variables
        /// </summary>
        protected virtual bool InitializeRoutineLocals { get { return true; } }

        #endregion
    }
}
