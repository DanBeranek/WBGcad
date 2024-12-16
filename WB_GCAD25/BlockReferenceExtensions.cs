using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.Runtime;

namespace WB_GCAD25
{
    public static class BlockReferenceExtensions
    {
   
        /// <summary>
        /// Creates and appends AttributeReferences to the
        /// BlockReference which the method is invoked on.
        /// </summary>
        /// <remarks>
        /// This method creates and appends all non-constant 
        /// attributes defined in the BlockTableRecord that
        /// is referenced by the given BlockReference.
        /// </remarks>
        /// <param name="blockRef">The BlockReference to which
        /// the attributes are to be added. The argument must 
        /// be database-resident and open for write.</param>
        /// <param name="tr">The Transaction to use in the
        /// operation, or null to use the current transaction
        /// of the Database containing the BlockReference.</param>
        /// <returns>An IDictionary containing the newly-created
        /// AttributeReferences keyed to the value of their Tag
        /// property.

        public static IDictionary<string, AttributeReference> AppendAttributes(
            this BlockReference blockRef, Transaction tr)
        {
            if(blockRef == null)
                throw new ArgumentNullException("blockRef");
            ErrorStatus.NoDatabase.Check(blockRef.Database != null);
            ErrorStatus.NotOpenForWrite.Check(blockRef.IsWriteEnabled);
            tr = tr ?? blockRef.Database.TransactionManager.TopTransaction;
            ErrorStatus.NoActiveTransactions.Check(tr != null);
            RXClass rxclass = RXObject.GetClass(typeof(AttributeDefinition));
            var result = new Dictionary<string, AttributeReference>(
                StringComparer.InvariantCultureIgnoreCase);
            var btr = (BlockTableRecord) tr.GetObject(
                blockRef.DynamicBlockTableRecord, OpenMode.ForRead);
            if(!btr.HasAttributeDefinitions)
                return result;
            var xform = blockRef.BlockTransform;
            var collection = blockRef.AttributeCollection;
            foreach(ObjectId id in btr)
            {
                if(id.ObjectClass.IsDerivedFrom(rxclass))
                {
                    var attdef = (AttributeDefinition) tr.GetObject(id, OpenMode.ForRead);
                    if(!attdef.Constant)
                    {
                        var attref = new AttributeReference();
                        try
                        {
                            attref.SetAttributeFromBlock(attdef, xform);
                            if (attdef.HasFields)
                            {
                                Field field = tr.GetObject(attref.GetField(), OpenMode.ForWrite) as Field;
                                if (field == null)
                                {
                                    Active.Editor.WriteMessage($"Field could not be updated for tag: {attdef.Tag}.");
                                    continue;
                                }

                                string currentFieldCode =
                                    field.GetFieldCode(FieldCodeFlags.AddMarkers | FieldCodeFlags.FieldCode);
                                string prefix = "";
                                string newFieldCode = prefix + ExtractFieldFormula(currentFieldCode);

                                Field replaceField = new Field(newFieldCode);

                                if (replaceField.EvaluationStatus.Status == FieldEvaluationStatus.Success)
                                {
                                    attref.SetField(replaceField);
                                    tr.AddNewlyCreatedDBObject(replaceField, true);
                                    replaceField.Evaluate(16, null);
                                }
                                else
                                {
                                    Active.Editor.WriteMessage(
                                        $"\nField evaluation failed. Status: {replaceField.EvaluationStatus.Status}.");
                                }

                            }
                            collection.AppendAttribute(attref);
                            tr.AddNewlyCreatedDBObject(attref, true);
                            result.Add(attref.Tag, attref);
                        }
                        catch
                        {
                            attref.Dispose();
                            throw;
                        }
                    }
                }
            }
            return result;
        }
   
        /// <summary>
        /// Extracts the field formula from the input string by removing specific prefixes.
        /// </summary>
        /// <param name="input">The input string containing the field code.</param>
        /// <returns>The cleaned field formula.</returns>
        public static string ExtractFieldFormula(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var prefixRegex = new Regex(@"^[+\-]|^%%p", RegexOptions.Compiled);
            input = prefixRegex.Replace(input, string.Empty, 1);

            int startIdx = input.IndexOf("%<\\AcExpr");
            return startIdx != -1 ? input.Substring(startIdx) : input;
        }
        
    }

    public static class RuntimeExtensions
    {
        public static void Check(this ErrorStatus es, bool condition, string message = null)
        {
            if(!condition)
            {
                if(!string.IsNullOrEmpty(message))
                    throw new Gssoft.Gscad.Runtime.Exception(es, message);
                else
                    throw new Gssoft.Gscad.Runtime.Exception(es);
            }
        }
    }
}