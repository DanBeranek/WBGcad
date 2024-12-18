using System;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.GraphicsInterface;

namespace WB_GCAD25
{
    public class MiakoArrayDrawJig : DrawJig
    {
        #region Fields

        private int _mCurJigFactorNumber = 1;
        private readonly int _mTotalJigFactorCount = 3;

        private Point3d _mInsertPt;    // Factor #1
        private Point3d _mEndPt;  // Factor #2
        private Point3d _mWidthPt; // Factor #3
        private string _miakoName;
        private MiakoData.MiakoInfo _info;
        private int _nRows;
        private int _nCols;
        private double _mRotation;

        private BlockTableRecord _blockDef; // Cached Block Definition

        #endregion

        #region Constructors

        public MiakoArrayDrawJig(string miakoName)
        {
            _miakoName = miakoName;
            _info = MiakoData.Blocks[miakoName];
            
            Active.UsingTranscation(tr =>
            {
                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(_miakoName))
                {
                    Active.Editor.WriteMessage($"\nBlock {_miakoName} not found.");
                    tr.Abort();
                }
                _blockDef = (BlockTableRecord)tr.GetObject(bt[_miakoName], OpenMode.ForRead);
            });
        }

        #endregion

        #region Overrides

        protected override bool WorldDraw(WorldDraw draw)
        {
            Vector3d dirX = _mEndPt - _mInsertPt;
            Vector3d dirW = _mWidthPt - _mInsertPt;
            Vector3d dirY = dirW.ProjectTo(dirX, dirX);
            
            double lengthX = dirX.Length;
            double lengthY = dirY.Length;
            
            double spacingX = _info.Length_mm;
            double spacingY = _info.Width_mm;
            
            _mRotation = Vector3d.XAxis.GetAngleTo(dirX, Vector3d.ZAxis);
            _nCols = (int)Math.Ceiling((lengthX - 125) / spacingX);
            _nRows = (int)Math.Floor(lengthY / spacingY);
            if (_nCols < 1) _nCols = 1;
            if (_nRows < 1) _nRows = 1;

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
                    for (int c = 0; c < _nCols; c++)
                    {
                        cellPt = _mInsertPt
                                 + dirX.GetNormal() * spacingX * c;
                        tempBlock = new BlockReference(cellPt, _blockDef.ObjectId);
                        tempBlock.Rotation = _mRotation;
                        draw.Geometry.Draw(tempBlock);
                        tempBlock.Dispose();
                    }
                    break;
                case 3:
                    for (int c = 0; c < _nCols; c++)
                    {
                        for (int r = 0; r < _nRows; r++)
                        {
                            cellPt = _mInsertPt
                                     + dirX.GetNormal() * spacingX * c
                                     + dirY.GetNormal() * spacingY * r;
                            
                            tempBlock = new BlockReference(cellPt, _blockDef.ObjectId);
                            tempBlock.Rotation = _mRotation;
                            draw.Geometry.Draw(tempBlock);
                            tempBlock.Dispose();
                        }
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
                    JigPromptPointOptions prOptions2 = new JigPromptPointOptions("\nDélka pole: ");
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
                case 3:
                    JigPromptPointOptions prOptions3 = new JigPromptPointOptions("\nŠířka pole: ");
                    prOptions3.UseBasePoint = true;
                    prOptions3.BasePoint = _mInsertPt;
                    PromptPointResult prResult3 = prompts.AcquirePoint(prOptions3);
                    if (prResult3.Status == PromptStatus.Cancel) 
                        return SamplerStatus.Cancel;

                    if (prResult3.Value.Equals(_mWidthPt))
                    {
                        return SamplerStatus.NoChange;
                    }
                    _mWidthPt = prResult3.Value;
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

        #region Helper Functions

        private void CreateArray()
        {
            try
            {
                Active.UsingTranscation(tr =>
                {
                    BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(Active.Database.CurrentSpaceId, OpenMode.ForWrite);
                    
                    Vector3d dirX = _mEndPt - _mInsertPt;
                    Vector3d dirW = _mWidthPt - _mInsertPt;
                    Vector3d dirY = dirW.ProjectTo(dirX, dirX);
            
                    double lengthX = dirX.Length;
                    double lengthY = dirY.Length;
            
                    double spacingX = _info.Length_mm;
                    double spacingY = _info.Width_mm;
            
                    _mRotation = Vector3d.XAxis.GetAngleTo(dirX, Vector3d.ZAxis);
                    _nCols = (int)Math.Ceiling((lengthX - 125) / spacingX);
                    _nRows = (int)Math.Floor(lengthY / spacingY);
                    if (_nCols < 1) _nCols = 1;
                    if (_nRows < 1) _nRows = 1;

                    for (int c = 0; c < _nCols; c++)
                    {
                        for (int r = 0; r < _nRows; r++)
                        {
                            Point3d cellPt = _mInsertPt
                                     + dirX.GetNormal() * spacingX * c
                                     + dirY.GetNormal() * spacingY * r;
                            
                            BlockReference newBlock = new BlockReference(cellPt, _blockDef.ObjectId);
                            newBlock.Rotation = _mRotation;
                            currentSpace.AppendEntity(newBlock);
                            Helpers.SetLayer("WB_MIAKO", newBlock);
                            tr.AddNewlyCreatedDBObject(newBlock, true);
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