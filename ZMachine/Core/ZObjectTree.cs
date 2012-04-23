namespace JCowgill.ZMachine.Core
{
    /// <summary>
    /// Provides methods used to manipulate the z machine object tree
    /// </summary>
    public class ZObjectTree
    {
        private readonly MemoryBuffer memory;
        private readonly bool useLargeObjects;
        
        private readonly int propertyDefaultsBase;
        private readonly int objectTreeBase;

        private const int ObjectTypeParent = 0;
        private const int ObjectTypeSibling = 1;
        private const int ObjectTypeChild = 2;
        
        /// <summary>
        /// Creates a new object tree manager using the given backing memory
        /// </summary>
        /// <param name="buf">memory buffer to read tree from</param>
        public ZObjectTree(MemoryBuffer buf)
        {
            //Setup buffer
            this.memory = buf;
            propertyDefaultsBase = buf.GetUShort(0xA);
            useLargeObjects = buf.GetByte(0) >= 4;
            objectTreeBase = propertyDefaultsBase + (useLargeObjects ? 31 : 63);
            
            //Base must be after header
            if(propertyDefaultsBase < 64)
            {
                throw new ZMachineException("base of the object tree is within the header");
            }
        }
        
        /// <summary>
        /// Gets the address (in the z machine) of the given object number
        /// </summary>
        /// <param name="obj">object number</param>
        private int GetObjectAddress(int obj)
        {
            //Validate object number
            if(obj < 1 || obj > (useLargeObjects ? 255 : 65535))
            {
                throw new ZMachineException("attempt to access illegal object with number: " + obj.ToString());
            }
            
            //Return computed address
            return objectTreeBase + (obj - 1) * (useLargeObjects ? 9 : 14);
        }
        
        /// <summary>
        /// Gets a pointer contained within the object structure
        /// </summary>
        /// <param name="address">address of object</param>
        /// <param name="ptr">pointer to read</param>
        /// <return>the object number referenced in the pointer</return>
        private int GetObjectPointer(int address, int ptr)
        {
            if(useLargeObjects)
            {
                return memory.GetUShort(address + 6 + ptr * 2);
            }
            else
            {
                return memory.GetByte(address + 4 + ptr);
            }
        }
        
        /// <summary>
        /// Sets a pointer contained within the object structure
        /// </summary>
        /// <param name="address">address of object</param>
        /// <param name="ptr">pointer to set</param>
        /// <param name="value">value to set to</param>
        private void SetObjectPointer(int address, int ptr, int value)
        {
            if(useLargeObjects)
            {
                memory.SetUShort(address + 6 + ptr * 2, (ushort) value);
            }
            else
            {
                memory.SetByte(address + 4 + ptr, (byte) value);
            }
        }
        
        /// <summary>
        /// Gets the parent object of the object given
        /// </summary>
        /// <param name="obj">object number</param>
        /// <return>the parent object number</return>
        public int GetParent(int obj)
        {
            return GetObjectPointer(GetObjectAddress(obj), ObjectTypeParent);
        }
        
        /// <summary>
        /// Gets the next sibling of the object given
        /// </summary>
        /// <param name="obj">object number</param>
        /// <return>the sibling object number</return>
        public int GetSibling(int obj)
        {
            return GetObjectPointer(GetObjectAddress(obj), ObjectTypeSibling);
        }
        
        /// <summary>
        /// Gets the child object of the object given
        /// </summary>
        /// <param name="obj">object number</param>
        /// <return>the child object number</return>
        public int GetChild(int obj)
        {
            return GetObjectPointer(GetObjectAddress(obj), ObjectTypeChild);
        }
        
        /// <summary>
        /// Sets the parent of an object
        /// </summary>
        /// <param name="obj">object to modify</param>
        /// <param name="parent">new parent of obj</param>
        /// <remarks>
        /// <para>obj will become the first child of parent</para>
        /// <para>if parent = 0, obj is detached from the tree and will have no parent</para>
        /// </remarks>
        public void SetParent(int obj, int parent)
        {
            //Get object address + current parent
            int address = GetObjectAddress(obj);
            int oldParent = GetObjectPointer(obj, ObjectTypeParent);
            int oldSibling = GetObjectPointer(obj, ObjectTypeSibling);
            
            //Changed?
            if(oldParent == parent)
            {
                //No change
                return;
            }
            
            //Detach from previous parent
            if(oldParent != 0)
            {
                //Find myself in the chain of siblings
                int oldParentAddr = GetObjectAddress(oldParent);
                int child = GetObjectPointer(oldParentAddr, ObjectTypeChild);
                
                if(child == obj)
                {
                    //Detach parent's child
                    SetObjectPointer(oldParentAddr, ObjectTypeChild, GetSibling(child));
                }
                else
                {
                    //Must be a sibling
                    int prevSibling = child;
                    int sibling = GetSibling(child);
                    
                    while(sibling != obj)
                    {
                        //If sibling is 0, this is an error
                        if(sibling == 0)
                        {
                            throw new ZMachineException("object tree is not well-founded (failed to find object in the parent's children list)");
                        }
                        
                        //Get next sibling
                        prevSibling = sibling;
                        sibling = GetSibling(sibling);
                    }
                    
                    //Set previous sibling's next sibling
                    SetObjectPointer(prevSibling, ObjectTypeSibling, oldSibling);
                }
            }
            
            //Set object's new parent
            SetObjectPointer(obj, ObjectTypeParent, 0);
            
            //Atttach to new parent
            if(parent == 0)
            {
                //Wipe sibling
                SetObjectPointer(obj, ObjectTypeSibling, 0);
            }
            else
            {
                //parent's first child = us
                int parentAddr = GetObjectAddress(parent);
                
                //sibling = parent's first child
                SetObjectPointer(obj, ObjectTypeSibling, GetObjectPointer(parentAddr, ObjectTypeChild));
                
                //parent's first child = us
                SetObjectPointer(parentAddr, ObjectTypeChild, obj);
            }
        }
        
        /// <summary>
        /// Gets the address of a Z encoded string containing the object's name
        /// </summary>
        /// <param name="obj">object number</param>
        /// <return>address containing name of the object</return>
        public int GetObjectName(int obj)
        {
            int address = GetObjectAddress(obj);
            return memory.GetUShort(address + (useLargeObjects ? 12 : 7)) + 1;
        }
        
        /// <summary>
        /// Gets the VALUE of the default property entry for the given property number
        /// </summary>
        /// <param name="defProperty">property number</param>
        /// <returns>the value of the default property</returns>
        public ushort GetDefaultProperty(int defProperty)
        {
            //Validate range
            if (defProperty < 0 || defProperty > (useLargeObjects ? 31 : 63))
                throw new ZMachineException("invalid property number " + defProperty.ToString());

            //Get value
            return memory.GetUShort(propertyDefaultsBase + defProperty * 2);
        }
        
        /// <summary>
        /// Gets the address of the property table of an object
        /// </summary>
        /// <param name="obj">object number</param>
        /// <return>address containing name of the object</return>
        private int GetPropertyTableAddress(int obj)
        {
            //Get object address
            int address = GetObjectAddress(obj);
            
            //Get object name length
            int objNameStart = memory.GetUShort(address + (useLargeObjects ? 12 : 7));
            
            //Add object length to get property table address
            int propTable = objNameStart + 1 + memory.GetByte(objNameStart) * 2;
            
            //Validate
            if(propTable < 64)
            {
                throw new ZMachineException("property table for object " + obj.ToString() + " is in the header");
            }
            
            return propTable;
        }

        /// <summary>
        /// Gets the number of the property at the given address
        /// </summary>
        /// <param name="address">address of property</param>
        /// <returns>the number of the property</returns>
        private int GetPropertyNumber(int address)
        {
            return memory.GetByte(address) & (useLargeObjects ? 0x1F : 0x3F);
        }

        /// <summary>
        /// Gets the size of the property pointed to by the address
        /// </summary>
        /// <param name="address">address to size byte of property</param>
        /// <returns>the size of the property</returns>
        public int GetPropertyLength(int address)
        {
            byte byte1 = memory.GetByte(address);

            if (useLargeObjects)
            {
                //2 bytes?
                if ((byte1 & 0x80) != 0)
                {
                    //Yes
                    int size = memory.GetByte(address + 1) & 0x3F;

                    //Size 0 is treated as size 64
                    if (size == 0)
                        size = 64;

                    return size;
                }
                else
                {
                    //No - use 1 or 2 byte length indicator
                    return (byte1 >> 6) + 1;
                }
            }
            else
            {
                //Get size from first byte
                return (byte1 >> 5) + 1;
            }
        }

        /// <summary>
        /// Gets the address of a property within an object
        /// </summary>
        /// <param name="obj">object number</param>
        /// <param name="propNum">property number</param>
        /// <return>address of property (or 0 if not found)</return>
        private int FindProperty(int obj, int propNum)
        {
            //Get start of property table
            int address = GetPropertyTableAddress(obj);

            //Find property
            while (GetPropertyNumber(address) > propNum)
            {
                address += GetPropertyLength(address);
            }

            //Found?
            return (GetPropertyNumber(address) == propNum) ? address : 0;
        }
        
        /// <summary>
        /// Gets the address of the property data with the given property number
        /// </summary>
        /// <param name="obj">object number</param>
        /// <param name="propNum">property number</param>
        /// <return>address of property data (or 0 if not found)</return>
        public int GetPropertyAddress(int obj, int propNum)
        {
            //Get start of property table
            int address = GetPropertyTableAddress(obj);

            //Find property
            while (GetPropertyNumber(address) > propNum)
            {
                address += GetPropertyLength(address);
            }

            //Found?
            if (GetPropertyNumber(address) == propNum)
            {
                return address + (useLargeObjects ? 2 : 1);
            }
            else
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Gets the next property an object has
        /// </summary>
        /// <param name="obj">object number</param>
        /// <param name="propNum">property number (0 = get first property)</param>
        /// <return>number of the next property (0 = no more properties)</return>
        public int GetNextProperty(int obj, int propNum)
        {
            //Get start of property table
            int address = GetPropertyTableAddress(obj);

            //First property?
            if (propNum == 0)
            {
                return GetPropertyNumber(address);
            }

            //Find property
            while (GetPropertyNumber(address) > propNum)
            {
                address += GetPropertyLength(address);
            }

            //Select the one after this
            address += GetPropertyLength(address);
            return GetPropertyNumber(address);
        }

        /// <summary>
        /// Sets the value of a property (must be length 1 or 2)
        /// </summary>
        /// <param name="obj">object number</param>
        /// <param name="propNum">property number</param>
        /// <param name="value">property value</param>
        /// <remarks>
        /// This method throws if the property is the wrong size / does not exist
        /// </remarks>
        public void PutProperty(int obj, int propNum, ushort value)
        {
            //Get start of property table
            int address = GetPropertyTableAddress(obj);

            //Find property
            while (GetPropertyNumber(address) > propNum)
            {
                address += GetPropertyLength(address);
            }

            //Found?
            if (GetPropertyNumber(address) == propNum)
            {
                //Store value
                switch (GetPropertyLength(address))
                {
                    case 1:
                        memory.SetByte(address + 1, (byte) value);
                        break;

                    case 2:
                        memory.SetUShort(address + 1, value);
                        break;

                    default:
                        throw new ZMachineException("you cannot change a property more than 2 bytes long");
                }
            }
            else
            {
                //Property does not exist
                throw new ZMachineException("property " + propNum.ToString() +
                    " of object " + obj.ToString() + " does not exist");
            }
        }

        /// <summary>
        /// Gets the value of an attribute
        /// </summary>
        /// <param name="obj">object number</param>
        /// <param name="attribute">attribute number</param>
        public bool GetAttribute(int obj, int attribute)
        {
            //Validate attribute
            if(attribute < 0 || attribute >= (useLargeObjects ? 32 : 48))
            {
                throw new ZMachineException("attribute number " + attribute.ToString() + " out of range");
            }

            //Get attribute entry
            int address = GetObjectAddress(obj) + attribute / 8;

            //Return value
            return (memory.GetByte(address) & (0x80 >> (attribute % 8))) != 0;
        }

        /// <summary>
        /// Sets the value of an attribute
        /// </summary>
        /// <param name="obj">object number</param>
        /// <param name="attribute">attribute number</param>
        /// <param name="value">new attribute value</param>
        public void SetAttribute(int obj, int attribute, bool value)
        {
            //Validate attribute
            if (attribute < 0 || attribute > (useLargeObjects ? 31 : 63))
            {
                throw new ZMachineException("attribute number " + attribute.ToString() + " out of range");
            }

            //Get attribute entry
            int address = GetObjectAddress(obj) + attribute / 8;
            byte byteData = memory.GetByte(address);

            //Set value
            byte bit = (byte) (0x80 >> (attribute % 8));

            if (value)
            {
                byteData |= bit;
            }
            else
            {
                byteData &= (byte) ~bit;
            }

            //Store in memory
            memory.SetByte(address, byteData);
        }
    }
}
