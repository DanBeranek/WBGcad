using System;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
    public static class PolylineAnalyzer
    {
        // <summary>
        // Finds the pair of neighboring segments with the maximum combined length 
        // in a closed polyline defined by the given vertices.
        // </summary>
        // <param name="polyline">Polyline.</param>
        // <returns>A tuple of indices of the first, second & third point.</returns>
        public static (int, int, int) FindLongestNeighbourSegments(Polyline polyline)
        {
            if (polyline == null)
            {
                throw new ArgumentException("Polyline cannot be null");
            }

            if (!polyline.Closed)
            {
                throw new ArgumentException("Polyline must be closed");
            }
            
            int nVertices = polyline.NumberOfVertices;
            
            if (nVertices <= 2)
            {
                throw new ArgumentException("Polyline must have at least 2 vertices");
            }
            // Compute lengths of each polyline segment
            double[] lengths = new double[nVertices];
            for (int i = 0; i < nVertices; i++)
            {
                Point2d pt1 = polyline.GetPoint2dAt(i);
                Point2d pt2 = polyline.GetPoint2dAt((i + 1) % nVertices); // next vertex, wrap around
                double x1 = pt1.X;
                double y1 = pt1.Y;
                double x2 = pt2.X;
                double y2 = pt2.Y;
                lengths[i] = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            }
            
            // Find the maximum combined length of neighboring segments
            double maxCombined = double.MinValue;
            int maxIndex = -1;

            for (int i = 0; i < nVertices; i++)
            {
                double combined = lengths[i] + lengths[(i + 1) % nVertices];
                if (combined > maxCombined)
                {
                    maxCombined = combined;
                    maxIndex = i;
                }
            }
            
            int idx1 = (maxIndex + 0) % nVertices;
            int idx2 = (maxIndex + 1) % nVertices;
            int idx3 = (maxIndex + 2) % nVertices;
            
            return (idx1, idx2, idx3);
        }
    }
}