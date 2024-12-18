using System;
using System.Collections.Generic;
using System.Linq;
using Gssoft.Gscad.BoundaryRepresentation;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.Internal;

namespace WB_GCAD25
{
    public class Commands
    {
        [CommandMethod("NACTIMIAKO")]
        public void LoadMiakoBlocks()
        {
            NODHelper.UserPromptData data = NODHelper.LoadUserPromptsFromNOD();
            
            bool validInput = false;
            double thickness = 0.0;
            while (!validInput)
            {
                PromptDoubleOptions pdo = new PromptDoubleOptions("\nZadej tloušťku stropu: ");
                pdo.AllowNone = false;
                pdo.AllowArbitraryInput = false;
                pdo.DefaultValue = data.LastThickness;
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
            
            data.LastThickness = thickness;
            data.SlabThickness = thickness;
            data.IsBN = false;

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
                    Keywords = { "SN", "BN" },
                    AllowNone = false
                };
                pko.Keywords.Default = "SN";
                PromptResult pr = Active.Editor.GetKeywords(pko);
                if (pr.Status != PromptStatus.OK)
                {
                    Active.Editor.WriteMessage("\nPříkaz zrušen ");
                    return;
                }
                
                switch (pr.StringResult.ToUpper())
                {
                    case "SN":
                        importer.ImportBlocks("D:\\_WB - kopie\\vlozky_250.dwg");
                        concreteThickness = thickness - 190.0;
                        break;
                    case "BN":
                        importer.ImportBlocks("D:\\_WB - kopie\\vlozky_BNK.dwg");
                        concreteThickness = 0.0;
                        data.IsBN = true;
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
            
            NODHelper.SaveUserPromptToNOD(data);
        }

        [CommandMethod("POTPOLE")]
        public void PlacePOTBeams()
        {
            SpanPromptResult promptResult = SpanPrompter.PromptResult();
            
            POTPlacer potPlacer = new POTPlacer();
            IntervalResult intervalResult = potPlacer.PlaceOptimalBeams(promptResult);
            
            MiakoPlacer miakoPlacer = new MiakoPlacer(intervalResult, promptResult);
        }

        [CommandMethod("VENCOVKY")]
        public void PlaceVT()
        {
            NODHelper.UserPromptData data = NODHelper.LoadUserPromptsFromNOD();
            double slabThickness = data.SlabThickness;

            string blockName = null;
            switch (slabThickness)
            {
                case 210.0:
                    blockName = "VT_8_210";
                    break;
                case 250.0:
                    blockName = "VT_8_250";
                    break;
                case 290.0:
                    blockName = "VT_8_290";
                    break;
                default:
                    Active.Editor.WriteMessage($"\nTloušťka stropu je: {slabThickness}. Nebyla nalezena vhodná věncovka.");
                    break;
            }

            if (blockName == null) return;

            try
            {
                VTArrayDrawJig jigger = new VTArrayDrawJig(blockName);
                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    if (jigger.Jig())
                    {
                        tr.Commit();
                    }
                    else
                    {
                        tr.Abort();
                    }
                }
            }
            catch (SystemException ex)
            {
                Active.Editor.WriteMessage(ex.ToString());
            }
            
            NODHelper.SaveUserPromptToNOD(data);
        }
        
        [CommandMethod("MIAKO")]
        public void PlaceMiakoArray()
        {
            string blockName = GetMiakoBlockName();

            if (blockName == null) return;

            try
            {
                MiakoArrayDrawJig jigger = new MiakoArrayDrawJig(blockName);
                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    if (jigger.Jig())
                    {
                        tr.Commit();
                    }
                    else
                    {
                        tr.Abort();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Active.Editor.WriteMessage(ex.ToString());
            }
        }

        [CommandMethod("CHANGEMIAKO")]
        public void ChangeMiako()
        {
            string miakoHeight = GetMiakoHeight();
            if (miakoHeight == null) return;
            
            string miakoBlockName500 = miakoHeight.Contains("PULENA")
                ? "MIAKO_80_500_PULENA"
                : $"MIAKO_{miakoHeight}_500";
            
            string miakoBlockName625 = miakoHeight.Contains("PULENA")
                ? "MIAKO_80_625_PULENA"
                : $"MIAKO_{miakoHeight}_625";
            
            Active.UsingTranscation(tr =>
            {
                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                if (bt == null)
                {
                    Active.Editor.WriteMessage("\nUnable to access the Block Table.");
                    tr.Abort();
                    return;
                }

                // Check if the required blocks exist
                bool blocksExist = true;

                if (!bt.Has(miakoBlockName500))
                {
                    Active.Editor.WriteMessage($"\nBlock {miakoBlockName500} not found.");
                    blocksExist = false;
                }

                if (!bt.Has(miakoBlockName625))
                {
                    Active.Editor.WriteMessage($"\nBlock {miakoBlockName625} not found.");
                    blocksExist = false;
                }

                if (!blocksExist)
                {
                    tr.Abort();
                    return;
                }
                BlockTableRecord miako500 = (BlockTableRecord)tr.GetObject(bt[miakoBlockName500], OpenMode.ForRead);
                BlockTableRecord miako625 = (BlockTableRecord)tr.GetObject(bt[miakoBlockName625], OpenMode.ForRead);
                
                if (miako500 == null || miako625 == null)
                {
                    Active.Editor.WriteMessage("\nUnable to access one or both BlockTableRecords.");
                    tr.Abort();
                    return;
                }
                
                PromptSelectionResult promptResult = Active.Editor.GetSelection();
                
                if (promptResult.Status != PromptStatus.OK)
                {
                    Active.Editor.WriteMessage("\nNo objects selected.");
                    tr.Abort();
                    return;
                }
                
                if (promptResult.Status == PromptStatus.OK)
                {
                    
                    SelectionSet sSet = promptResult.Value;

                    foreach (SelectedObject so in sSet)
                    {
                        if (so == null) continue;

                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                        if (obj is BlockReference br)
                        {
                            // Get current block name
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                            string currentBlockName = btr.Name;

                            if (currentBlockName != null && currentBlockName.Contains("MIAKO"))
                            {
                                BlockTableRecord sourceBlockTableRecord = null;
                                if (currentBlockName.Contains("500"))
                                {
                                    sourceBlockTableRecord = miako500;
                                }
                                else
                                {
                                    sourceBlockTableRecord = miako625;
                                }
                                
                                BlockReference targetBlockReference = (BlockReference)tr.GetObject(br.ObjectId, OpenMode.ForWrite);
                                targetBlockReference.BlockTableRecord = sourceBlockTableRecord.ObjectId;
                            }
                        }
                    }
                }
            });
        }

        private static string GetMiakoOVN()
        {
            NODHelper.UserPromptData data = NODHelper.LoadUserPromptsFromNOD();
            
            PromptKeywordOptions pko = new PromptKeywordOptions("\nVyber OVN: ")
            {
                Keywords = { "625", "500" },
                AllowNone = false
            };
            pko.Keywords.Default = data.LastOVN;
            
            PromptResult pr = Active.Editor.GetKeywords(pko);
            
            if (pr.Status != PromptStatus.OK)
            {
                Active.Editor.WriteMessage("\nPříkaz zrušen ");
                return null;
            }
            
            string OVN = pr.StringResult;
            data.LastOVN = OVN;
            
            NODHelper.SaveUserPromptToNOD(data);
            
            return OVN;
        }

        private static string GetMiakoHeight()
        {
            NODHelper.UserPromptData data = NODHelper.LoadUserPromptsFromNOD();
            
            PromptKeywordOptions pko = new PromptKeywordOptions("\nVyber výšku vložky: ")
            {
                Keywords = { "PULENA80", "80", "150" },
                AllowNone = false,
                AllowArbitraryInput = true,
            };
            if (data.SlabThickness >= 250.0) pko.Keywords.Add("190");
            if (data.SlabThickness >= 290.0) pko.Keywords.Add("230");
            if (data.IsBN) pko.Keywords.Add("250");
            
            string[] keywords = new string[pko.Keywords.Count];
            
            for (int i = 0; i < pko.Keywords.Count; i++)
            {
                keywords[i] = pko.Keywords[i].GlobalName;
            }

            if (keywords.Contains(data.LastMiako))
            {
                pko.Keywords.Default = data.LastMiako;
            }
            else
            {
                pko.Keywords.Default = keywords.Last();
            }
            
            PromptResult pr = Active.Editor.GetKeywords(pko);
            
            if (pr.Status != PromptStatus.OK)
            {
                Active.Editor.WriteMessage("\nPříkaz zrušen ");
                return null;
            }
            
            string miakoHeight = pr.StringResult;
            data.LastMiako = miakoHeight;
            
            NODHelper.SaveUserPromptToNOD(data);

            return miakoHeight;
        }
        
        private static string GetMiakoBlockName()
        {
            string OVN = GetMiakoOVN();
            string miakoHeight = GetMiakoHeight();

            if (miakoHeight == null || OVN == null)
            {
                Active.Editor.WriteMessage("\nPříkaz zrušen ");
                return null;
            }
            
            string blockName = miakoHeight.Contains("PULENA")
                ? $"MIAKO_80_{OVN}_PULENA"
                : $"MIAKO_{miakoHeight}_{OVN}";

            return blockName;
        }

        [CommandMethod("KRESLIDESKU")]
        public void DrawSlab()
        {
            BulgePolyJig jig = new BulgePolyJig(Active.Editor.CurrentUserCoordinateSystem);
            Polyline pl = jig.RunBulgePolyJig();
            if (pl == null) return; 
            Helpers.SetLayer("WB_DESKA_OBRYS", pl);
            CustomDataFunctions.StoreKeyValue(pl.ObjectId, "TLOUSTKA", 250.0);
        }
        
        [CommandMethod("KRESLIPROSTUP")]
        public void DrawHole()
        {
            BulgePolyJig jig = new BulgePolyJig(Active.Editor.CurrentUserCoordinateSystem);
            Polyline pl = jig.RunBulgePolyJig();
            if (pl == null) return;
            
            Helpers.SetLayer("WB_DESKA_PROSTUP", pl);
            CustomDataFunctions.StoreKeyValue(pl.ObjectId, "TLOUSTKA", 250.0);
            
            (int idx1, int idx2, int idx3) = PolylineAnalyzer.FindLongestNeighbourSegments(pl);
            // Get points
            Point2d pt1 = pl.GetPoint2dAt(idx1);
            Point2d pt2 = pl.GetPoint2dAt(idx2);
            Point2d pt3 = pl.GetPoint2dAt(idx3);

            Vector2d a = new Vector2d(pt2.X - pt1.X, pt2.Y - pt1.Y);
            Vector2d b = new Vector2d(pt2.X - pt3.X, pt2.Y - pt3.Y);
            
            Vector2d bisectorVector = (a.GetNormal() + b.GetNormal()).GetNormal();
            double distance = Math.Min(a.Length, b.Length) / 5;

            Point2d pt4 = pt2 + bisectorVector * distance;

            PointContainment ptContainment = Containment.GetPointContainment(pl, new Point3d(new Plane(), pt4));

            if (ptContainment != PointContainment.Inside)
            {
                pt4 = pt2 - bisectorVector * distance;
            }
            
            Polyline hatchPl = null;
            Active.UsingTranscation(tr =>
            {
                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                hatchPl = new Polyline();
                hatchPl.AddVertexAt(0, pt1, 0, 0, 0);
                hatchPl.AddVertexAt(0, pt2, 0, 0, 0);
                hatchPl.AddVertexAt(0, pt3, 0, 0, 0);
                hatchPl.AddVertexAt(0, pt4, 0, 0, 0);
                hatchPl.Closed = true;
                btr.AppendEntity(hatchPl);
                tr.AddNewlyCreatedDBObject(hatchPl, true);
            });
            Helpers.SetLayer("WB_DESKA_PROSTUP_SRAFA", hatchPl);
            ObjectIdCollection objIds = new ObjectIdCollection();
            objIds.Add(hatchPl.Id);

            Hatch hatch = null;
            Active.UsingTranscation(tr =>
            {
                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                
                hatch = new Hatch();
                
                btr.AppendEntity(hatch);
                tr.AddNewlyCreatedDBObject(hatch, true);
                
                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                hatch.Associative = true;
                hatch.AppendLoop(HatchLoopTypes.Default, objIds);
                hatch.EvaluateHatch(true);
            });
            Helpers.SetLayer("WB_DESKA_PROSTUP_SRAFA", hatch);
        }

        [CommandMethod("NASTAVTLOUSTKU")]
        public void SetThickness()
        {
            NODHelper.UserPromptData data = NODHelper.LoadUserPromptsFromNOD();
            
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nZadej tloušťku v mm: ");
            pdo.AllowNegative = false;
            pdo.AllowZero = false;
            pdo.DefaultValue = data.LastThickness;

            PromptDoubleResult pdr = Active.Editor.GetDouble(pdo);

            double thickness = pdr.Value;
            data.LastThickness = thickness;
            
            PromptSelectionResult psr = Active.Editor.GetSelection();
            if (psr.Status != PromptStatus.OK)
            {
                Active.Editor.WriteMessage("\nNo selection provided.");
                return;
            }
            SelectionSet ss = psr.Value;
            foreach (SelectedObject selObj in ss)
            {
                if (selObj == null) continue; ;
                CustomDataFunctions.StoreKeyValue(selObj.ObjectId, "TLOUSTKA", thickness);
            }
            
            NODHelper.SaveUserPromptToNOD(data);
        }
        
        [CommandMethod("UKAZTLOUSTKU")]
        public void ShowThickness()
        {
            PromptSelectionResult psr = Active.Editor.GetSelection();
            if (psr.Status != PromptStatus.OK)
            {
                Active.Editor.WriteMessage("\nNo selection provided.");
                return;
            }
            SelectionSet ss = psr.Value;
            foreach (SelectedObject selObj in ss)
            {
                if (selObj == null) continue; ;
                double thick = (double)CustomDataFunctions.GetValue(selObj.ObjectId, "TLOUSTKA");
                
                Active.Editor.WriteMessage($"\nTloušťka: {thick} mm.");
            }
        }

        [CommandMethod("CTIUSERPROMPTDATA")]
        public void ReadUserPromptData()
        {
            NODHelper.WriteUserPromptToConsole();
        }
    }
}