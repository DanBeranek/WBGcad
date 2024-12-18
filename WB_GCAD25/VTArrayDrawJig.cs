using System;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.DatabaseServices.Filters;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.GraphicsInterface;

namespace WB_GCAD25
{
    public class VTArrayDrawJig : DrawJig
    {
        #region Fields

        private int _mCurJigFactorNumber = 1;
        private readonly int _mTotalJigFactorCount = 2;
        
        private Point3d _mInsertPt;    // Factor #1
        private Point3d _mEndPt;  // Factor #2
        private string _vtName;
        private int _n;
        private double _mRotation;
        
        private BlockTableRecord _blockDef; // Cached Block Definition
        
        #endregion

        #region Constructors

        public VTArrayDrawJig(string vtName)
        {
            _vtName = vtName;
            
            Active.UsingTranscation(tr =>
            {
                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(_vtName))
                {
                    Active.Editor.WriteMessage($"\nBlock {_vtName} not found.");
                    tr.Abort();
                }
                _blockDef = (BlockTableRecord)tr.GetObject(bt[_vtName], OpenMode.ForRead);
            });
        }

        #endregion

        #region Overrides

        protected override bool WorldDraw(WorldDraw draw)
        {
            Vector3d dir = _mEndPt - _mInsertPt;
            double length = dir.Length;
            _mRotation = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
            _n = (int)Math.Ceiling(length / 500.0);
            if (_n < 1) _n = 1;

            Point3d cellPt;
            BlockReference tempBlock;
            
            switch (_mCurJigFactorNumber)
            {
                case 1:
                    tempBlock = new BlockReference(_mInsertPt, _blockDef.ObjectId);
                    draw.Geometry.Draw(tempBlock);
                    tempBlock.Dispose();
                    break;
                case 2:
                    for (int c = 0; c < _n; c++)
                    {
                        cellPt = _mInsertPt
                                 + dir.GetNormal() * 500.0 * c;
                        tempBlock = new BlockReference(cellPt, _blockDef.ObjectId);
                        tempBlock.Rotation = _mRotation;
                        draw.Geometry.Draw(tempBlock);
                        tempBlock.Dispose();
                    }
                    break;
            }


            return true;
        }
        
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            switch (_mCurJigFactorNumber)
            {
                case 1:
                    JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nBod vložení: ");
                    PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
                    if (prResult1.Status == PromptStatus.Cancel) return SamplerStatus.Cancel;

                    if (prResult1.Value.Equals(_mInsertPt))
                    {
                        return SamplerStatus.NoChange;
                    }
                    _mInsertPt = prResult1.Value;
                    return SamplerStatus.OK;
                    
                case 2:
                    JigPromptPointOptions prOptions2 = new JigPromptPointOptions("\nDélka: ");
                    prOptions2.UseBasePoint = true;
                    prOptions2.BasePoint = _mInsertPt;
                    PromptPointResult prResult2 = prompts.AcquirePoint(prOptions2);
                    if (prResult2.Status == PromptStatus.Cancel) 
                        return SamplerStatus.Cancel;

                    if (prResult2.Value.Equals(_mEndPt))
                    {
                        return SamplerStatus.NoChange;
                    }
                    _mEndPt = prResult2.Value;
                    return SamplerStatus.OK;
                default:
                    break;
            }

            return SamplerStatus.OK;
        }

        #endregion

        #region Method to Call

        public bool Jig()
        {
            try
            {
                PromptResult pr;
                do
                {
                    pr = Active.Editor.Drag(this);
                    if (pr.Status == PromptStatus.Keyword)
                    {
                        // Keyword handling code
                    }
                    else
                        _mCurJigFactorNumber++;
                } while ((pr.Status != PromptStatus.Cancel && pr.Status != PromptStatus.Error)
                         && _mCurJigFactorNumber <= _mTotalJigFactorCount);

                if (_mCurJigFactorNumber == _mTotalJigFactorCount + 1)
                {
                    CreateArray();
                    return true;
                }
                return false;
            }
            catch {return false;}
        }

        #endregion

        #region Helpers

        private void CreateArray()
        {
            try
            {
                Active.UsingTranscation(tr =>
                {
                    BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(Active.Database.CurrentSpaceId, OpenMode.ForWrite);
                    
                    Vector3d dir = _mEndPt - _mInsertPt;
                    double length = dir.Length;
                    _mRotation = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
                    _n = (int)Math.Ceiling(length / 500.0);
                    if (_n < 1) _n = 1;
                    
                    Point3d cellPt;
                    BlockReference tempBlock;
                    for (int c = 0; c < _n; c++)
                    {
                        cellPt = _mInsertPt
                                 + dir.GetNormal() * 500.0 * c;
                        
                        BlockReference newBlock = new BlockReference(cellPt, _blockDef.ObjectId);
                        newBlock.Rotation = _mRotation;
                        currentSpace.AppendEntity(newBlock);
                        Helpers.SetLayer("WB_VENCOVKY", newBlock);
                        tr.AddNewlyCreatedDBObject(newBlock, true);

                        if (c == _n - 1)
                        {
                            bool lastClipped = Math.Abs(length % 500) > 1e-4;
                            if (lastClipped)
                            {
                                Matrix3d mat = newBlock.BlockTransform;
                                mat.Inverse();
                                
                                Point2dCollection ptCol = new Point2dCollection();
                                
                                Point3d pt1 = new Point3d(0, 0, 0);
                                Point3d pt2 = new Point3d(length % 500, 80, 0);
                                pt1.TransformBy(mat);
                                pt2.TransformBy(mat);
                                ptCol.Add(new Point2d(pt1.X, pt1.Y));
                                ptCol.Add(new Point2d(pt2.X, pt2.Y));
                                
                                // Set the clipping boundary and enable it
                                using (SpatialFilter filter = new SpatialFilter())
                                {
                                    SpatialFilterDefinition filterDef =
                                        new SpatialFilterDefinition(ptCol, Vector3d.ZAxis, 0, 0, 0, true);
                                    filter.Definition = filterDef;

                                    // Define the name of the extension dictionary and entry name
                                    string dictName = "ACAD_FILTER";
                                    string spName = "SPATIAL";

                                    // Check to see if the Extension Dictionary exists, if not, create it
                                    if (newBlock.ExtensionDictionary.IsNull)
                                    {
                                        newBlock.CreateExtensionDictionary();
                                    }

                                    // Open the dictionary to write
                                    DBDictionary extDict = (DBDictionary)tr.GetObject(newBlock.ExtensionDictionary, OpenMode.ForWrite);
                                    
                                    // Check to see if the dictionary for clipped boundaries exists, 
                                    // and add the spatial filter to the dictionary
                                    if (extDict.Contains(dictName))
                                    {
                                        DBDictionary filterDict = (DBDictionary)tr.GetObject(extDict.GetAt(dictName), OpenMode.ForWrite);

                                        if (filterDict.Contains(spName))
                                        {
                                            filterDict.Remove(spName);
                                        }

                                        filterDict.SetAt(spName, filter);
                                    }
                                    else
                                    {
                                        using (DBDictionary filterDict = new DBDictionary())
                                        {
                                            extDict.SetAt(dictName, filterDict);
                                            tr.AddNewlyCreatedDBObject(filterDict, true);
                                            filterDict.SetAt(spName, filter);
                                        }
                                    }
                                    
                                    //Append the spatial filter to the drawing
                                    tr.AddNewlyCreatedDBObject(filter, true);
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        #endregion
        
    }
}