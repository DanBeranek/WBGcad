using System.IO;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.Runtime;

namespace WB_GCAD25
{
    public class BlockImporter
    {
        public void ImportBlocks(string filename)
        {
            Database sourceDb = new Database(false, true);
            try
            {
                // Read the DWG into side database
                sourceDb.ReadDwgFile(filename, FileShare.Read, true, "");

                // Create a variable to store the list of block identifiers
                ObjectIdCollection blockIds = new ObjectIdCollection();
                TransactionManager tm = sourceDb.TransactionManager;
                using (Transaction myT = tm.StartTransaction())
                {
                    // Open the block table
                    BlockTable bt =
                        (BlockTable)tm.GetObject(sourceDb.BlockTableId,
                            OpenMode.ForRead,
                            false);

                    // Check each block in the block table
                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr =
                            (BlockTableRecord)tm.GetObject(btrId,
                                OpenMode.ForRead,
                                false);
                        // Only add named & non-layout blocks to the copy list
                        if (!btr.IsAnonymous && !btr.IsLayout)
                            blockIds.Add(btrId);
                        btr.Dispose();
                    }
                }
                // Copy blocks from source to destination database
                IdMapping mapping = new IdMapping();
                sourceDb.WblockCloneObjects(blockIds,
                    Active.Database.BlockTableId,
                    mapping,
                    DuplicateRecordCloning.Replace,
                    false);

                Active.Editor.WriteMessage("\nCopied "
                                           + blockIds.Count.ToString()
                                           + " block definitions from "
                                           + filename
                                           + " to the current drawing.");
            }
            catch(Exception ex)
            {
                Active.Editor.WriteMessage("\nError during copy: " + ex.Message);
            }
            sourceDb.Dispose();
        }
    }
}