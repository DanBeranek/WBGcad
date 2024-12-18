using Gssoft.Gscad.DatabaseServices;

namespace WB_GCAD25
{
    public static class NODHelper
    {
        private const string NOD_RECORD_NAME = "WBCAD_UserPrompts";

        public class UserPromptData
        {
            public double SlabThickness { get; set; } = 250.0;
            public double LastThickness { get; set; } = 250.0;
            public string LastOVN { get; set; } = "625";
            public string LastMiako { get; set; } = "190";
            public bool IsBN { get; set; } = false;
            public double LastInsulationThickness { get; set; } = 120.0;
        }

        public static void SaveUserPromptToNOD(UserPromptData data)
        {
            Active.UsingTranscation(tr =>
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(Active.Database.NamedObjectsDictionaryId, OpenMode.ForRead);

                Xrecord xRec;
                if (!nod.Contains(NOD_RECORD_NAME))
                {
                    nod.UpgradeOpen();
                    xRec = new Xrecord();
                    ObjectId xRecId = nod.SetAt(NOD_RECORD_NAME, xRec);
                    tr.AddNewlyCreatedDBObject(xRec, true);
                }
                else
                {
                    xRec = (Xrecord)tr.GetObject(nod.GetAt(NOD_RECORD_NAME), OpenMode.ForWrite);
                }

                using (ResultBuffer buffer = new ResultBuffer(
                           new TypedValue((int)DxfCode.Real, data.SlabThickness),
                           new TypedValue((int)DxfCode.Real, data.LastThickness),
                           new TypedValue((int)DxfCode.Text, data.LastOVN),
                           new TypedValue((int)DxfCode.Text, data.LastMiako),
                           new TypedValue((int)DxfCode.Int16, data.IsBN ? 1 : 0),
                           new TypedValue((int)DxfCode.Real, data.LastInsulationThickness)
                       ))
                {
                    xRec.Data = buffer;
                }
            });
        }

        public static UserPromptData LoadUserPromptsFromNOD()
        {
            UserPromptData result = new UserPromptData();
            
            Active.UsingTranscation(tr =>
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(Active.Database.NamedObjectsDictionaryId, OpenMode.ForRead);

                if (nod.Contains(NOD_RECORD_NAME))
                {
                    Xrecord xRec = (Xrecord)tr.GetObject(nod.GetAt(NOD_RECORD_NAME), OpenMode.ForRead);
                    ResultBuffer rb = xRec.Data;

                    if (rb != null)
                    {
                        TypedValue[] values = rb.AsArray();
                        if (values.Length > 2)
                        {
                            result.SlabThickness = (double)values[0].Value;
                            result.LastThickness = (double)values[1].Value;
                            result.LastOVN = (string)values[2].Value;
                            result.LastMiako = (string)values[3].Value;
                            result.IsBN = (short)values[4].Value == 1;
                            result.LastInsulationThickness = (double)values[5].Value;
                        }
                    }
                }
            });
            return result;
        }

        public static void WriteUserPromptToConsole()
        {
            UserPromptData data = LoadUserPromptsFromNOD();
            
            Active.Editor.WriteMessage($"\n--- UserPromptData ---");
            Active.Editor.WriteMessage($"\nSlabThickness: {data.SlabThickness}");
            Active.Editor.WriteMessage($"\nLastThickness: {data.LastThickness}");
            Active.Editor.WriteMessage($"\nLastOVN: {data.LastOVN}");
            Active.Editor.WriteMessage($"\nLastMiako: {data.LastMiako}");
            Active.Editor.WriteMessage($"\nIsBN: {data.IsBN}");
            Active.Editor.WriteMessage($"\nLastInsulationThickness: {data.LastInsulationThickness}");
        }
        
    }
}