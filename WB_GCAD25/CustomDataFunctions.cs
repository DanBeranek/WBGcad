using Gssoft.Gscad.DatabaseServices;

namespace WB_GCAD25
{
    public static class CustomDataFunctions
    {
        /// <summary>
        /// Stores a value of various supported types (string, double, int) in an entity's extension dictionary under the specified key.
        /// If the key already exists, it will be overwritten.
        /// </summary>
        /// <param name="objectId">The ObjectId of the entity.</param>
        /// <param name="key">A string key identifying the data.</param>
        /// <param name="value">The value to store (can be string, double, int, etc.).</param>
        public static void StoreKeyValue(ObjectId objectId, string key, object value)
        {
            // Determine the appropriate DxfCode and actual value to store
            TypedValue typedVal = GetTypedValueForObject(value);
            
            Active.UsingTranscation(tr =>
            {
                Entity entity = (Entity)tr.GetObject(objectId, OpenMode.ForWrite);
                
                if (entity == null)
                {
                    return;
                }

                if (!entity.ExtensionDictionary.IsValid)
                {
                    entity.CreateExtensionDictionary();
                }

                DBDictionary extDict = (DBDictionary)tr.GetObject(entity.ExtensionDictionary, OpenMode.ForWrite);

                Xrecord xRec = new Xrecord { Data = new ResultBuffer(typedVal) };
                
                if (extDict.Contains(key))
                {
                    ObjectId xRecId = extDict.GetAt(key);
                    Xrecord existingXRec = (Xrecord)tr.GetObject(xRecId, OpenMode.ForWrite);
                    existingXRec.Data = xRec.Data;
                }
                else
                {
                    extDict.SetAt(key, xRec);
                    tr.AddNewlyCreatedDBObject(xRec, true);
                }
            });
        }

        /// <summary>
        /// Retrieves a stored value for the given key. This will return the raw object as stored.
        /// </summary>
        /// <param name="objectId">The ObjectId of the entity.</param>
        /// <param name="key">The key under which the data was stored.</param>
        /// <returns>The stored object or null if not found.</returns>
        public static object GetValue(ObjectId objectId, string key)
        {
            object result = null;
            Active.UsingTranscation(tr =>
            {
                Entity ent = (Entity)tr.GetObject(objectId, OpenMode.ForRead);

                if (ent == null || !ent.ExtensionDictionary.IsValid) 
                    return;
                
                DBDictionary extDict = (DBDictionary)tr.GetObject(ent.ExtensionDictionary, OpenMode.ForRead);
                
                if (!extDict.Contains(key)) 
                    return;
                
                ObjectId xRecId = extDict.GetAt(key);
                Xrecord xRec = (Xrecord)tr.GetObject(xRecId, OpenMode.ForRead);

                TypedValue[] values = xRec.Data.AsArray();
                if (values == null || values.Length == 0)
                {
                    return;
                }
                
                result = ConvertTypedValueToObject(values[0]);
            });
            return result;
        }
        
        /// <summary>
        /// Converts an object into a TypedValue with the appropriate DxfCode based on its type.
        /// </summary>
        private static TypedValue GetTypedValueForObject(object value)
        {
            if (value is string s)
            {
                return new TypedValue((int)DxfCode.Text, s);
            }
            if (value is double d)
            {
                return new TypedValue((int)DxfCode.Real, d);
            }
            if (value is int i)
            {
                return new TypedValue((int)DxfCode.Int32, i);
            }
            // If the type is not directly handled, convert it to a string
            return new TypedValue((int)DxfCode.Text, value.ToString());
        }
        
        /// <summary>
        /// Converts a TypedValue back into a .NET object.
        /// </summary>
        private static object ConvertTypedValueToObject(TypedValue tv)
        {
            switch ((DxfCode)tv.TypeCode)
            {
                case DxfCode.Text:
                    return (string)tv.Value;
                case DxfCode.Real:
                    return (double)tv.Value;
                case DxfCode.Int32:
                    return (int)tv.Value;
                default:
                    // If it's another type not handled, return it as-is
                    return tv.Value;
            }
        }
    }
}