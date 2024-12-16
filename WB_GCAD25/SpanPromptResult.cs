using System.Collections.Generic;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
    // Class to hold all prompt results
    public class SpanPromptResult
    {
        public Point3d Pt1 { get; set; }
        public Point3d Pt2 { get; set; }
        public Point3d Pt3 { get; set; }
        public List<double> XLoads { get; set; }
        public double MaxOvn { get; set; }
        public bool DoubledBeams { get; set; }
        public bool OmitFirstBeam { get; set; }
        public bool OmitLastBeam { get; set; }
        public bool BeamPlacementStart { get; set; }
        public bool BeamPlacementEnd { get; set; }

        public double RoomLength
        {
            get
            {
                return Pt1.GetVectorTo(Pt2).Length;
            }
        }

        public double RoomWidth
        {
            get
            {
                return Pt1.GetVectorTo(Pt3).Length;
            }
        }

        public double BeamLength
        {
            get
            {
                double length = RoomLength;
                if (BeamPlacementStart) length += 125.0;
                if (BeamPlacementEnd) length += 125.0;
                return Utils.CeilToBase(length, 250.0);
            }
        }

        public double PlacementOffset
        {
            get
            {
                double off = 0;
                if (BeamPlacementStart && BeamPlacementEnd)
                {
                    off = (BeamLength - RoomLength) / 2;
                }
                else if (BeamPlacementStart)
                {
                    off = BeamLength - RoomLength;
                }
                return off;
            }
        }

        public Vector3d DirX
        {
            get
            {
                return Pt1.GetVectorTo(Pt3).GetNormal();
            }
        }

        public Vector3d DirY
        {
            get
            {
                return Pt1.GetVectorTo(Pt2).GetNormal();
            }
        }
    }
}