using System;
using System.Collections.Generic;
using System.Globalization;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
    public class POTPlacer
    {
        public IntervalResult PlaceOptimalBeams(SpanPromptResult pmtResult)
        {
            // Call FindOptimalIntervals
                IntervalFinder finder = new IntervalFinder();

                IntervalResult result = finder.FindOptimalIntervals(
                    pmtResult.MaxOvn, 
                    pmtResult.RoomWidth,
                    pmtResult.DoubledBeams,
                    pmtResult.XLoads,
                    pmtResult.OmitFirstBeam,
                    pmtResult.OmitLastBeam
                    );

                // Place beams based on x_coords
                PlaceBeams(result.XCoords, pmtResult.Pt1, pmtResult.Pt2, pmtResult.Pt3, pmtResult.BeamLength, pmtResult.PlacementOffset);
                
                return result;
        }

        private void PlaceBeams(List<double> xCoords, Point3d pt1, Point3d pt2, Point3d pt3, double beamsLength, double placementOffset)
        {
            if (xCoords.Count == 0)
            {
                Active.Editor.WriteMessage("\nNebyly definovány souřadnice nosníků. Nosníky nebyly umístěny.");
            }

            Vector3d dirX = pt1.GetVectorTo(pt3).GetNormal();
            Vector3d dirY = pt1.GetVectorTo(pt2).GetNormal();
            
            Active.UsingTranscation(tr =>
            {
                BlockTable blockTable = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                if (!blockTable.Has("POT NOSNÍK"))
                {
                    throw new System.Exception("Blok 'POT NOSNÍK' nebyl nalezen!");
                }
                ObjectId blockRecId = blockTable["POT NOSNÍK"];
                    
                // Insert the block into the current space
                foreach (double x in xCoords)
                {
                    Point3d insertPt = pt1
                                       + dirX * x
                                       - dirY * placementOffset;
                    if (blockRecId != ObjectId.Null)
                    {
                        using (BlockReference blockRef = new BlockReference(insertPt, blockRecId))
                        {
                            modelSpace.AppendEntity(blockRef);
                            blockRef.Rotation = dirY.GetAngleTo(new Vector3d(1, 0, 0));
                            Helpers.SetLayer("WB_POT_OSA", blockRef);
                            Helpers.SetDynamicBlockProperty("DELKA", beamsLength, blockRef);
                            
                            tr.AddNewlyCreatedDBObject(blockRef, true);
                            // blockRef.AppendAttributes(tr);
                        }
                    }
                }
            });
        }
    }
}