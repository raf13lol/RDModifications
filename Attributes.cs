using System;

namespace RDModifications
{
    public class BaseModificationAttribute(bool autoPatch = true) : Attribute 
    {
        public bool autoPatch = autoPatch;
    }
    
    public class ModificationAttribute(bool autoPatch = true) : BaseModificationAttribute(autoPatch) 
    {
    }

    public class EditorModificationAttribute(bool autoPatch = true) : BaseModificationAttribute(autoPatch) 
    {
    }
}