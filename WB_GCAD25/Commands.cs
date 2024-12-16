using System.Collections.Generic;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
    public class Commands
    {
        [CommandMethod("NACTIMIAKO")]
        public void LoadMiakoBlocks()
        {
            bool validInput = false;
            double thickness = 0.0;
            while (!validInput)
            {
                PromptDoubleOptions pdo = new PromptDoubleOptions("\nZadej tloušťku stropu: ");
                pdo.AllowNone = false;
                pdo.AllowArbitraryInput = false;
                pdo.DefaultValue = 250.0;
                pdo.UseDefaultValue = true;
                PromptDoubleResult pdr = Active.Editor.GetDouble(pdo);
                if (pdr.Status != PromptStatus.OK)
                {
                    Active.Editor.WriteMessage("\nPříkaz zrušen");
                    return;
                }

                thickness = pdr.Value;

                if (thickness < 200.0 || thickness > 400.0)
                {
                    Active.Editor.WriteMessage("\nZadaná hodnota je mimo rozsah [200.0; 400.0]. Zkus to znovu.");
                }
                else
                {
                    validInput = true;
                }
            }

            double concreteThickness = 60.0;
            BlockImporter importer = new BlockImporter();
            if (thickness < 250.0)
            {
                importer.ImportBlocks("D:\\_WB - kopie\\vlozky_210.dwg");
                concreteThickness = thickness - 150.0;
            }
            else if (thickness == 250.0)
            {
                PromptKeywordOptions pko = new PromptKeywordOptions("\nVyber typ stropu: ")
                {
                    Keywords = { "S nadbetonávkou", "Bez nadbetonávky" },
                    AllowNone = false
                };
                pko.Keywords.Default = "S nadbetonávkou";
                PromptResult pr = Active.Editor.GetKeywords(pko);
                if (pr.Status != PromptStatus.OK)
                {
                    Active.Editor.WriteMessage("\nPříkaz zrušen ");
                    return;
                }

                switch (pr.StringResult)
                {
                    case "S nadbetonávkou":
                        importer.ImportBlocks("D:\\_WB - kopie\\vlozky_250.dwg");
                        concreteThickness = thickness - 190.0;
                        break;
                    case "Bez nadbetonávky":
                        importer.ImportBlocks("D:\\_WB - kopie\\vlozky_BNK.dwg");
                        concreteThickness = 0.0;
                        break;
                }
            }
            else if (thickness < 290.0)
            {
                importer.ImportBlocks("D:\\_WB - kopie\\vlozky_250.dwg");
                concreteThickness = thickness - 190.0;
            }
            else
            {
                importer.ImportBlocks("D:\\_WB - kopie\\vlozky_290.dwg");
                concreteThickness = thickness - 230.0;
            }

            DatabaseSummaryInfo info = Active.Database.SummaryInfo;
            DatabaseSummaryInfoBuilder builder = new DatabaseSummaryInfoBuilder(info);
            IDictionary<string, string> dict = (IDictionary<string, string>)builder.CustomPropertyTable;
            if (dict.ContainsKey("STROP"))
            {
                dict["STROP"] = thickness.ToString("F0");
            }
            else
            {
                dict.Add("STROP", thickness.ToString("F0"));
            }
            if (dict.ContainsKey("NADBETONAVKA"))
            {
                dict["NADBETONAVKA"] = concreteThickness.ToString("F0");
            }
            else
            {
                dict.Add("NADBETONAVKA", concreteThickness.ToString("F0"));
            }

            DatabaseSummaryInfo newInfo = builder.ToDatabaseSummaryInfo();
            Active.Database.SummaryInfo = newInfo;
        }

        [CommandMethod("POTPOLE")]
        public void PlacePOTBeams()
        {
            SpanPromptResult promptResult = SpanPrompter.PromptResult();
            
            POTPlacer potPlacer = new POTPlacer();
            IntervalResult intervalResult = potPlacer.PlaceOptimalBeams(promptResult);
            
            MiakoPlacer miakoPlacer = new MiakoPlacer(intervalResult, promptResult);
        }
        
        [CommandMethod("KRESLIDESKU")]
        public void DrawSlab()
        {
            BulgePolyJig jig = new BulgePolyJig(Active.Editor.CurrentUserCoordinateSystem);
            Polyline pl = jig.RunBulgePolyJig();
            Helpers.SetLayer("WB_DESKA_OBRYS", pl);
        }
        
        [CommandMethod("KRESLIPROSTUP")]
        public void DrawHole()
        {
            BulgePolyJig jig = new BulgePolyJig(Active.Editor.CurrentUserCoordinateSystem);
            Polyline pl = jig.RunBulgePolyJig();

            (int idx1, int idx2, int idx3) = PolylineAnalyzer.FindLongestNeighbourSegments(pl);
            // Helpers.SetLayer("WB_DESKA_PROSTUP", pl);
            // PRO SRAFU: WB_DESKA_PROSTUP_SRAFA
        }
        
        
        
        [CommandMethod("AddExtensionDictionary")]
        public void AddExtensionDictionary()
        {
            Active.UsingTranscation(tr =>
            {
                PromptEntityOptions prompt = new PromptEntityOptions("\nDo you want to add extension dictionary?");
                PromptEntityResult result = Active.Editor.GetEntity(prompt);

                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                
                PromptDoubleOptions pDoOpt = new PromptDoubleOptions("\nHow much?");
                PromptDoubleResult pResult = Active.Editor.GetDouble(pDoOpt);

                if (pResult.Status != PromptStatus.OK)
                {
                    return;
                }

                Entity ent = (Entity)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

                if (!ent.ExtensionDictionary.IsValid)
                {
                    ent.CreateExtensionDictionary();
                }
                
                DBDictionary extDict = (DBDictionary)tr.GetObject(ent.ExtensionDictionary, OpenMode.ForWrite);
                
                Xrecord xrecord = new Xrecord
                {
                    Data = new ResultBuffer(
                        new TypedValue((int)DxfCode.Real, pResult.Value)
                    )
                };
                extDict.SetAt("SlabHeight", xrecord);
                tr.AddNewlyCreatedDBObject(ent, true);
            });
        }

        [CommandMethod("GetExtensionDictionary")]
        public void GetExtensionDictionary()
        {
            Active.UsingTranscation(tr =>
            {
                PromptEntityOptions prompt = new PromptEntityOptions("\nDo you want to add extension dictionary?");
                PromptEntityResult result = Active.Editor.GetEntity(prompt);

                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Entity ent = (Entity)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

                if (ent.ExtensionDictionary.IsValid)
                {
                    DBDictionary extDict = (DBDictionary)tr.GetObject(ent.ExtensionDictionary, OpenMode.ForRead);
                    if (extDict.Contains("SlabHeight"))
                    {
                        Xrecord xrecord = (Xrecord)tr.GetObject(extDict.GetAt("SlabHeight"), OpenMode.ForRead);

                        if (xrecord != null)
                        {
                            foreach (TypedValue val in xrecord.Data)
                            {
                                if (val.TypeCode == (int)DxfCode.Real)
                                {
                                    Active.Editor.WriteMessage($"\nSlab height: {val.Value}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Active.Editor.WriteMessage("\nNo slab height data found");
                    }
                }
            });
        }
        
        
    }
}