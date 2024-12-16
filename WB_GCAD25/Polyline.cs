using System.Collections.Generic;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
    public class Polyline
    {
        public void CreatePolylineWithXDict(string layerName)
        {
            PolylineJig jig = new PolylineJig();

            PromptResult pr;
            do
            {
                pr = jig.Drag();
            } while (pr.Status == PromptStatus.OK);

            if (pr.Status == PromptStatus.Cancel || jig.Points.Count < 2)
            {
                Active.Editor.WriteMessage("\nNo valid polyline created.");
            }
            
            // User finished the jig, now we have a polyline with all the points
            
            Active.UsingTranscation(tr =>
            {
                Gssoft.Gscad.DatabaseServices.Polyline polyline = new Gssoft.Gscad.DatabaseServices.Polyline();
                for (int i = 0; i < jig.Points.Count; i++)
                {
                    polyline.AddVertexAt(i, jig.Points[i].Convert2d(new Plane()), 0, 0, 0);
                }
                polyline.Closed = true;
                Helpers.SetLayer(layerName, polyline);

                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ObjectId plId = btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);
            });
        }
        
        public class PolylineJig : EntityJig
        {
            public List<Point3d> Points { get; private set; }
            private int _currentIndex;

            public PolylineJig() : base(new Gssoft.Gscad.DatabaseServices.Polyline())
            {
                Points = new List<Point3d>();
                _currentIndex = -1;
            }
            
            // Call this method in a loop until the user completes the polyline
            public PromptResult Drag()
            {
                // If this is the first point, ask for the starting point
                if (Points.Count == 0)
                {
                    PromptPointOptions ppo = new PromptPointOptions("\nSpecify start point or [Cancel]:");
                    PromptPointResult ppr = Active.Editor.GetPoint(ppo);
                    if (ppr.Status != PromptStatus.OK) return null;
                    Points.Add(ppr.Value);
                    _currentIndex = Points.Count;
                }
                
                // Now we want the next point
                PromptPointOptions ppoNext = new PromptPointOptions("\nSpecify next point or [Done]:");
                ppoNext.Keywords.Add("Done");
                PromptPointResult pprNext = Active.Editor.GetPoint(ppoNext);

                if (pprNext.Status == PromptStatus.None)
                {
                    return null;
                }
                if (pprNext.Status == PromptStatus.OK)
                {
                    Points.Add(pprNext.Value);
                    _currentIndex = Points.Count - 1;
    
                    // Run the jig
                    return Active.Editor.Drag(this);
                }
                return pprNext;
            }
            
            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                if (_currentIndex < 0 || _currentIndex >= Points.Count) return SamplerStatus.Cancel;
                
                // Get a temporary point to show dynamic preview
                JigPromptPointOptions ppo = new JigPromptPointOptions("\nSpecify next point: ");
                ppo.UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.NullResponseAccepted;

                PromptPointResult ppr = prompts.AcquirePoint(ppo);
                if (ppr.Status == PromptStatus.OK)
                {
                    Point3d newPoint = ppr.Value;
                    
                    // If point hasn't changed, return NoChange
                    if (newPoint == Points[_currentIndex])
                    {
                        return SamplerStatus.NoChange;
                    }
                    else
                    {
                        Points[_currentIndex] = newPoint;
                        return SamplerStatus.OK;
                    }
                }
                else
                {
                    return SamplerStatus.Cancel;
                }
            }

            protected override bool Update()
            {
                // Update the polyline geometry in the jig's entity
                Gssoft.Gscad.DatabaseServices.Polyline pl = Entity as Gssoft.Gscad.DatabaseServices.Polyline;
                if (pl == null) return false;
                
                pl.SetDatabaseDefaults();
                
                // Clear existing vertices
                while (pl.NumberOfVertices > 0)
                {
                    pl.RemoveVertexAt(pl.NumberOfVertices - 1);
                }
                
                // Add all points as vertices
                for (int i = 0; i < Points.Count; i++)
                {
                    pl.AddVertexAt(i, Points[i].Convert2d(new Plane()), 0, 0, 0);
                }
                return true;
            }
        }
    }
}