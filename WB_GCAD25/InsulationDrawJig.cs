using System;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.DatabaseServices.Filters;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.GraphicsInterface;

namespace WB_GCAD25
{
    public class InsulationDrawJig : DrawJig
    {
        #region Fields

        private int _mCurJigFactorNumber = 1;
        private readonly int _mTotalJigFactorCount = 2;

        private Point3d _mInsertPt;    // Factor #1
        private Point3d _mEndPt;  // Factor #2
        private double _scale = 120.0;
        private string _insulationType = "S";
        private string _insulationName;
        private double _mRotation;
        
        private BlockTableRecord _blockDef; // Cached Block Definition

        #endregion

        #region Constructors

        public InsulationDrawJig()
        {
            SetBlockDef();
        }

        #endregion

        #region Overrides

        protected override bool WorldDraw(WorldDraw draw)
        {
            Vector3d dir = _mEndPt - _mInsertPt;
            double length = dir.Length;
            _mRotation = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
            
            BlockReference tempBlock;
            
            switch (_mCurJigFactorNumber)
            {
                case 1:
                    tempBlock = new BlockReference(_mInsertPt, _blockDef.ObjectId);
                    tempBlock.ScaleFactors = new Scale3d(_scale, _scale, _scale);
                    draw.Geometry.Draw(tempBlock);
                    tempBlock.Dispose();
                    break;
                case 2:
                    tempBlock = new BlockReference(_mInsertPt, _blockDef.ObjectId);
                    tempBlock.Rotation = _mRotation;
                    tempBlock.ScaleFactors = new Scale3d(_scale, _scale, _scale);
                    draw.Geometry.Draw(tempBlock);
                    tempBlock.Dispose();
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
                    prOptions1.Keywords.Add("Typ");
                    prOptions1.Keywords.Add("Měřítko");
                    prOptions1.AppendKeywordsToMessage = true;
                    PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
                    if (prResult1.Status == PromptStatus.Cancel) 
                        return SamplerStatus.Cancel;

                    if (prResult1.Status == PromptStatus.Keyword)
                    {
                        HandleKeyword(prResult1.StringResult);
                        return SamplerStatus.NoChange;
                    }

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
                    prOptions2.Keywords.Add("Typ");
                    prOptions2.Keywords.Add("Měřítko");
                    prOptions2.AppendKeywordsToMessage = true;
                    PromptPointResult prResult2 = prompts.AcquirePoint(prOptions2);
                    if (prResult2.Status == PromptStatus.Cancel) 
                        return SamplerStatus.Cancel;
                    
                    if (prResult2.Status == PromptStatus.Keyword)
                    {
                        HandleKeyword(prResult2.StringResult);
                        return SamplerStatus.NoChange;
                    }

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
                        // Handle keywords
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

        private void HandleKeyword(string keyword)
        {
            switch (keyword.ToLower())
            {
                case "typ":
                    PromptKeywordOptions pko = new PromptKeywordOptions("\nVyber typ izolace: ");
                    pko.Keywords.Add("S");
                    pko.Keywords.Add("U");
                    pko.Keywords.Default = "S";
                    pko.AppendKeywordsToMessage = true;

                    PromptResult pRes = Active.Editor.GetKeywords(pko);
                    if (pRes.Status == PromptStatus.OK)
                    {
                        _insulationType = pRes.StringResult;
                        SetBlockDef();
                    }
                    break;
                case "měřítko":
                    PromptDoubleOptions pdo = new PromptDoubleOptions("\nZadejte tloušťku izolace: ");
                    pdo.DefaultValue = _scale;
                    PromptDoubleResult pRes2 = Active.Editor.GetDouble(pdo);
                    if (pRes2.Status == PromptStatus.OK)
                    {
                        _scale = pRes2.Value;
                    }
                    break;
            }
        }

        private void SetBlockDef()
        {
            _insulationName = $"TEPELNA IZOLACE_{_insulationType}";
            
            Active.UsingTranscation(tr =>
            {
                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(_insulationName))
                {
                    Active.Editor.WriteMessage($"\nBlock {_insulationName} not found.");
                    tr.Abort();
                }
                _blockDef = (BlockTableRecord)tr.GetObject(bt[_insulationName], OpenMode.ForRead);
            });
        }

        private void CreateArray()
        {
            try
            {
                Active.UsingTranscation(tr =>
                {
                    BlockTableRecord currentSpace =
                        (BlockTableRecord)tr.GetObject(Active.Database.CurrentSpaceId, OpenMode.ForWrite);
                        
                    Vector3d dir = _mEndPt - _mInsertPt;
                    double length = dir.Length;
                    _mRotation = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
                    
                    BlockReference newBlock = new BlockReference(_mInsertPt, _blockDef.ObjectId)
                    {
                        Rotation = _mRotation,
                        ScaleFactors = new Scale3d(_scale, _scale, _scale),
                    };
                    currentSpace.AppendEntity(newBlock);
                    Helpers.SetLayer("WB_IZOLACE", newBlock);
                    Helpers.SetDynamicBlockProperty("DELKA", Utils.CeilToBase(length, _scale), newBlock);
                    tr.AddNewlyCreatedDBObject(newBlock, true);
                    
                    Matrix3d mat = newBlock.BlockTransform;
                    mat.Inverse();
                                
                    Point2dCollection ptCol = new Point2dCollection();
                                
                    Point3d pt1 = new Point3d(0, 0, 0);
                    Point3d pt2 = new Point3d(length / _scale, 1, 0);
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
                });
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage($"\n{ex.Message}");
            }
        }

        #endregion
    }
}