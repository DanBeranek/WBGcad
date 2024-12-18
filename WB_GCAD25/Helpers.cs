using System;
using Gssoft.Gscad.DatabaseServices;

namespace WB_GCAD25
{
    public static class Helpers
    {
        public static void SetLayer(string layerName, Entity entity)
        {
            Active.UsingTranscation(tr =>
            {
                // Check if the layer already exists
                LayerTable table = (LayerTable)tr.GetObject(Active.Database.LayerTableId, OpenMode.ForWrite);

                if (!table.Has(layerName))
                {
                    throw new Exception($"\nLayer {layerName} not found");
                }

                Entity ent = (Entity)tr.GetObject(entity.ObjectId, OpenMode.ForWrite);
                ent.Layer = layerName;
            });
        }

        public static void SetDynamicBlockProperty(string propertyName, double value, BlockReference blockReference)
        {
            Active.UsingTranscation(tr =>
            {
                BlockReference br = (BlockReference)tr.GetObject(blockReference.ObjectId, OpenMode.ForWrite);
                
                DynamicBlockReferencePropertyCollection props = br.DynamicBlockReferencePropertyCollection;
                foreach (DynamicBlockReferenceProperty prop in props)
                {
                    if (prop.PropertyName == propertyName && !prop.ReadOnly)
                    {
                        prop.Value = value;
                    }
                }
            });
        }
    }
}