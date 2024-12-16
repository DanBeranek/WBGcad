// BrepSamples.cs (excerpt)  Copyright (c) 2008  Tony Tanzillo
// https://www.theswamp.org/index.php?topic=43572.msg488632#msg488632
using System;
using System.Linq;
using Gssoft.Gscad.BoundaryRepresentation;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.Runtime;
using AcBr = Gssoft.Gscad.BoundaryRepresentation;

namespace WB_GCAD25
{
    public static class Containment
    {
        // <summary>
        // 
        // This sample shows how to use the BREP API to do
        // simple topographical point/boundary classification
        // on regions.
        // 
        // This code requires a reference to AcDbMgdBrep.dll
        // 
        // The sample prompts for a region object; gets a BRep 
        // object representing the region; and then repeatedly
        // prompts for points, and computes and displays the
        // containment (inside, outside, or on an edge) of each
        // selected point.
        // 
        // A point within an inner loop is considered to be 
        // 'ouside' the region's area, and so if you perform 
        // this operation on a complex region with inner loops, 
        // points contained within an inner loop will yield a
        // result of PointContainment.Outside.
        //
        // This is the most robust means of determining if a
        // point lies within the implied boundary formed by a
        // collection of AutoCAD entities that can be used to 
        // create a valid AutoCAD Region object, and is the 
        // preferred means of doing simple point containment 
        // testing.
        // 
        // You can adapt this code to perform containment tests
        // on various AutoCAD entities such as closed polylines, 
        // splines, ellipses, etc., by generating a region from 
        // the closed geometry, using it to do the containment 
        // test, and then disposing it without having to add it
        // to a database.
        // 
        // Note that the sample code was designed to work with
        // regions that lie in the WCS XY plane and points that 
        // lie in the same plane. 
        // 
        // </summary>
 
        [CommandMethod( "CONTAINMENT" )]
        public static void ContainmentCommand()
        {
            Editor ed = Active.Editor;
            ObjectId id = GetRegion( ed, "\nSelect a region: " );
            if( !id.IsNull )
            {
                using( Transaction tr = ed.Document.TransactionManager.StartTransaction() )
                {
                    try
                    {
                        Region region = id.GetObject( OpenMode.ForRead ) as Region;
 
                        PromptPointOptions ppo = new PromptPointOptions( "\nSelect a point: " );
                        ppo.AllowNone = true;
 
                        while( true ) // loop while user continues to pick points
                        {
                            // Get a point from user:
                            PromptPointResult ppr = ed.GetPoint( ppo );
 
                            if( ppr.Status != PromptStatus.OK )  // no point was selected, exit
                                break;
 
                            // use the GetPointContainment helper method below to 
                            // get the PointContainment of the selected point:
                            PointContainment containment = GetPointContainment( region, ppr.Value );
 
                            // Display the result:
                            ed.WriteMessage( "\nPointContainment = {0}", containment.ToString() );
                        }
                    }
                    finally
                    {
                        tr.Commit();
                    }
                }
            }
        }
 
        /// <summary>
        /// 
        /// This variation performs point contianment testing
        /// against any closed curve from which a simple region 
        /// (e.g., no inner-loops) can be genreated, using the
        /// Region.CreateFromCurves() API.
        /// 
        /// </summary>
 
        [CommandMethod( "CURVECONTAINMENT" )]
        public static void CurveContainmentCommand()
        {
            Editor ed = Active.Editor;
            ObjectId id = GetCurve( ed, "\nSelect a closed Curve: " );
            if( !id.IsNull )
            {
                using( Transaction tr = ed.Document.TransactionManager.StartTransaction() )
                {
                    try
                    {
                        Curve curve = (Curve) id.GetObject( OpenMode.ForRead );
                        if( !curve.Closed )
                        {
                            ed.WriteMessage( "\nInvalid selection, requires a CLOSED curve" );
                            return;
                        }
 
                        using( Region region = RegionFromClosedCurve( curve ) )
                        {
                            PromptPointOptions ppo = new PromptPointOptions( "\nSelect a point: " );
                            ppo.AllowNone = true;
 
                            while( true ) // loop while user continues to pick points
                            {
                                // Get a point from user:
                                PromptPointResult ppr = ed.GetPoint( ppo );
 
                                if( ppr.Status != PromptStatus.OK )  // no point was selected, exit
                                    break;
 
                                // use the GetPointContainment helper method below to 
                                // get the PointContainment of the selected point:
                                PointContainment containment = GetPointContainment( region, ppr.Value );
 
                                // Display the result:
                                ed.WriteMessage( "\nPointContainment = {0}", containment.ToString() );
                            }
                        }
                    }
                    finally
                    {
                        tr.Commit();
                    }
                }
            }
        }
 
 
        // this helper method takes a Region and a Point3d that must be
        // in the plane of the region, and returns the PointContainment
        // of the given point, adjusted to correctly indicate its actual
        // relationship to the region (inside/on boundary edge/outside).
 
        public static PointContainment GetPointContainment( Region region, Point3d point )
        {
            PointContainment result = PointContainment.Outside;
 
            // Get a Brep object representing the region:
            using( Brep brep = new Brep( region ) )
            {
                if( brep != null )
                {
                    // Get the PointContainment and the BrepEntity at the given point:
 
                    using( BrepEntity ent = brep.GetPointContainment( point, out result ) )
                    {
                        // GetPointContainment() returns PointContainment.OnBoundary
                        // when the picked point is either inside the region's area
                        // or exactly on an edge.
                        // 
                        // So, to distinguish between a point on an edge and a point
                        // inside the region, we must check the type of the returned
                        // BrepEntity:
                        // 
                        // If the picked point was on an edge, the returned BrepEntity 
                        // will be an Edge object. If the point was inside the boundary,
                        // the returned BrepEntity will be a Face object.
                        //
                        // So if the returned BrepEntity's type is a Face, we return
                        // PointContainment.Inside:
 
                        if( ent is AcBr.Face )
                            result = PointContainment.Inside;
 
                    }
                }
            }
            return result;
        }
 
        public static Region RegionFromClosedCurve( Curve curve )
        {
            if( !curve.Closed )
                throw new ArgumentException( "Curve must be closed." );
            DBObjectCollection curves = new DBObjectCollection();
            curves.Add( curve );
            using( DBObjectCollection regions = Region.CreateFromCurves( curves ) )
            {
                if( regions == null || regions.Count == 0 )
                    throw new InvalidOperationException( "Failed to create regions" );
                if( regions.Count > 1 )
                    throw new InvalidOperationException( "Multiple regions created" );
                return regions.Cast<Region>().First();
            }
        }
 
        public static PointContainment GetPointContainment( Curve curve, Point3d point )
        {
            if( ! curve.Closed )
                throw new ArgumentException("Curve must be closed.");
            Region region = RegionFromClosedCurve( curve );
            if( region == null )
                throw new InvalidOperationException( "Failed to create region" );
            using( region )
            {
                return GetPointContainment( region, point );
            }
        }
 
        public static ObjectId GetCurve( Editor editor, string msg )
        {
            PromptEntityOptions peo = new PromptEntityOptions( msg );
            peo.SetRejectMessage( "Invalid selection: requires a closed Curve," );
            peo.AddAllowedClass( typeof( Curve ), false );
            PromptEntityResult res = editor.GetEntity( peo );
            return res.Status == PromptStatus.OK ? res.ObjectId : ObjectId.Null;
        }
   
        public static ObjectId GetRegion( Editor editor, string msg )
        {
            PromptEntityOptions peo = new PromptEntityOptions( msg );
            peo.SetRejectMessage( "Invalid selection: requires a region," );
            peo.AddAllowedClass( typeof( Region ), false );
            PromptEntityResult res = editor.GetEntity( peo );
            return res.Status == PromptStatus.OK ? res.ObjectId : ObjectId.Null;
        }
 
    }
}