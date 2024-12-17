using System;
using System.Collections.Generic;
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
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nZadej tloušťku v mm: ");
            pdo.AllowNegative = false;
            pdo.AllowZero = false;
            
            // Default value
            DatabaseSummaryInfo info = Active.Database.SummaryInfo;
            DatabaseSummaryInfoBuilder builder = new DatabaseSummaryInfoBuilder(info);
            IDictionary<string, string> dict = (IDictionary<string, string>)builder.CustomPropertyTable;
            pdo.DefaultValue = double.Parse(dict["STROP"]);

            PromptDoubleResult pdr = Active.Editor.GetDouble(pdo);

            double thickness = pdr.Value;
            
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
    }
}