using System.Collections.Generic;
using System.Globalization;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.GraphicsInterface;

namespace WB_GCAD25
{
    public class PotArrayDrawJig : DrawJig
    {
        #region Fields

        private int _mCurJigFactorNumber = 1;
        private readonly int _mTotalJigFactorCount = 4;

        private Point3d _mInsertPt; // Factor #1
        private Point3d _mEndPt; // Factor #2
        private Point3d _mWidthPt; // Factor #3
        
        // Can be changed by keywords
        private string _maxOVNString = "625"; 
        private List<double> _xLoads = new List<double>();
        private bool _doubledBeams = false;
        private bool _omitFirstBeam = false;
        private bool _omitLastBeam = false;
        private bool _beamStartPlacement = true;
        private bool _beamEndPlacement = true;

        // Will be calculated based on inputs
        private IntervalFinder _finder = new IntervalFinder();
        private IntervalResult _intervalResult;
        
        // Some shit calculated based on previous variables
        private Vector3d _dirBeams;
        private Vector3d _dirWidth;
        private Vector3d _dirPerp; // perpendicular to the _dirBeams
        private string _blockName = "POT NOSNÍK";
        private BlockTableRecord _blockDef;

        #endregion

        #region Properties

        private double RoomLength => _dirBeams.Length;
        private double RoomWidth => _dirPerp.Length;
        
        private Vector3d UnitDirBeams => _dirBeams.GetNormal();
        private Vector3d UnitDirPerp => _dirPerp.GetNormal();

        private double BeamLength
        {
            get
            {
                double length = RoomLength;
                if (_beamStartPlacement) length += 120.0;
                if (_beamEndPlacement) length += 120.0;
                return Utils.CeilToBase(length, 250.0);
            }
        }

        private double PlacementOffset
        {
            get
            {
                if (_beamStartPlacement && _beamEndPlacement) return (BeamLength - RoomLength) / 2;
                if (_beamStartPlacement) return BeamLength - RoomLength;
                return 0;
            }
        }

        private double MaxOVN
        {
            get
            {
                switch (_maxOVNString)
                {
                    case "625":
                        return 625.0;
                    case "625+500":
                        return 562.5;
                    case "500+625":
                        return 562.5;
                    case "500":
                        return 500.0;
                    default:
                        return 625.0;
                }
            }
        }
 

        #endregion

        #region Constructors

        public PotArrayDrawJig()
        {
            Active.UsingTranscation(tr =>
            {
                BlockTable bt = (BlockTable)tr.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(_blockName))
                {
                    Active.Editor.WriteMessage($"\nBlock {_blockName} not found.");
                    tr.Abort();
                }
                _blockDef = (BlockTableRecord)tr.GetObject(bt[_blockName], OpenMode.ForRead);
            });
        }

        #endregion

        #region Overrides

        protected override bool WorldDraw(WorldDraw draw)
        {
            _dirBeams = _mEndPt - _mInsertPt;
            _dirWidth = _mWidthPt - _mInsertPt;
            _dirPerp = _dirWidth.ProjectTo(_dirBeams, _dirBeams);

            Point3d insertPt, endPt;
            Point3d pt1, pt2, pt3, pt4;
            Line tempLine;
            
            switch (_mCurJigFactorNumber)
            {
                case 1:
                    break;
                case 2:
                    pt1 = _mInsertPt - UnitDirBeams * PlacementOffset;
                    pt2 = pt1 + UnitDirBeams * BeamLength;
                    tempLine = new Line(pt1, pt2);
                    draw.Geometry.Draw(tempLine);
                    tempLine.Dispose();
                    break;
                case 3:
                    pt1 = _mInsertPt - UnitDirBeams * PlacementOffset;
                    pt2 = pt1 + UnitDirBeams * BeamLength;
                    pt3 = pt2 + UnitDirPerp * RoomWidth;
                    pt4 = pt1 + UnitDirPerp * RoomWidth;
                    Line tempLine1 = new Line(pt1, pt2);
                    Line tempLine2 = new Line(pt2, pt3);
                    Line tempLine3 = new Line(pt3, pt4);
                    Line tempLine4 = new Line(pt4, pt1);
                    Line tempLine5 = new Line(pt1, pt3);
                    Line tempLine6 = new Line(pt2, pt4);
                    draw.Geometry.Draw(tempLine1);
                    draw.Geometry.Draw(tempLine2);
                    draw.Geometry.Draw(tempLine3);
                    draw.Geometry.Draw(tempLine4);
                    draw.Geometry.Draw(tempLine5);
                    draw.Geometry.Draw(tempLine6);
                    tempLine1.Dispose();
                    tempLine2.Dispose();
                    tempLine3.Dispose();
                    tempLine4.Dispose();
                    tempLine5.Dispose();
                    tempLine6.Dispose();
                    break;
                case 4:
                    _intervalResult = _finder.FindOptimalIntervals(MaxOVN, RoomWidth, _doubledBeams, _xLoads,
                        _omitFirstBeam, _omitLastBeam);

                    if (_intervalResult != null)
                    {
                        foreach (double x in _intervalResult.XCoords)
                        {
                            insertPt = _mInsertPt 
                                       + UnitDirPerp * x
                                       - UnitDirBeams * PlacementOffset;
                            endPt = insertPt 
                                    + UnitDirBeams * BeamLength;
                            
                            tempLine = new Line(insertPt, endPt);
                            draw.Geometry.Draw(tempLine);
                            tempLine.Dispose();
                        }
                    }
                    else
                    {
                        Active.Editor.WriteMessage($"\nNebylo nalezeno řešení. Uprav vstupy.");
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
                case 4:
                    JigPromptPointOptions prOptions4 = new JigPromptPointOptions("\nKliknutím potvrďte");
                    
                    prOptions4.Keywords.Add("OVN");
                    prOptions4.Keywords.Add("Přidej");
                    prOptions4.Keywords.Add("Zdvoj");
                    prOptions4.Keywords.Add("vynechPRvni");
                    prOptions4.Keywords.Add("vynechPOsledni");
                    prOptions4.Keywords.Add("ulozZAcatek");
                    prOptions4.Keywords.Add("ulozKOnec");

                    prOptions4.AppendKeywordsToMessage = true;
                    
                    PromptPointResult prResult4 = prompts.AcquirePoint(prOptions4);
                    if (prResult4.Status == PromptStatus.Cancel) 
                        return SamplerStatus.Cancel;
                    if (prResult4.Status == PromptStatus.Keyword)
                    {
                        HandleKeyword(prResult4.StringResult);
                        return SamplerStatus.OK;
                    }
                    return SamplerStatus.NoChange;
                default:
                    break;
            }
            
            return SamplerStatus.OK;
        }

        #endregion

        #region MethodToCall

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
                    // CreateArray();
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
            string msg = null;
            switch (keyword.ToLower())
            {
                case "ovn":
                    PromptKeywordOptions pko = new PromptKeywordOptions("\nVyber OVN: ");
                    pko.Keywords.Add("625");
                    pko.Keywords.Add("500+625");
                    pko.Keywords.Add("500");
                    pko.Keywords.Default = "625";
                    pko.AppendKeywordsToMessage = true;

                    PromptResult pRes = Active.Editor.GetKeywords(pko);
                    if (pRes.Status == PromptStatus.OK)
                    {
                        _maxOVNString = pRes.StringResult;
                        msg = $"\nOVN změněna na {_maxOVNString}.";
                    }
                    break;
                case "přidej":
                    bool continueSelecting = true;
                    while (continueSelecting)
                    {
                        PromptPointOptions ppo = new PromptPointOptions(
                            "\nVyberte bod, kam chcete přidat POT nosník, stiskněte Enter pro ukončení zadávání: ")
                        {
                            AllowNone = true,
                            UseBasePoint = true,
                            BasePoint = _mInsertPt
                        };
                        PromptPointResult ppr = Active.Editor.GetPoint(ppo);
                        if (ppr.Status == PromptStatus.OK)
                        {
                            // Calculate position
                            Point3d pt = ppr.Value;
                            Vector3d dir = pt - _mInsertPt;
                            double length = dir.ProjectTo(_dirBeams, _dirBeams).Length;
                            _xLoads.Add(length);
                            Active.Editor.WriteMessage($"\nNosník bude přidán ve vzdálenosti cca {(length/1000).ToString("F2", CultureInfo.InvariantCulture)} m.");
                        }
                        else if (ppr.Status == PromptStatus.None)
                        {
                            // User pressed Enter without selecting a point
                            continueSelecting = false;
                            Active.Editor.WriteMessage("\nKonec přidávání nosníků.");
                        }
                    }
                    break;
                case "zdvoj":
                    _doubledBeams = !_doubledBeams;
                    msg = _doubledBeams
                        ? "\nBudou použity zdvojené nosníky."
                        : "\nBudou použity jednoduché nosníky.";
                    break;
                case "vynechprvni":
                    _omitFirstBeam = !_omitFirstBeam;
                    msg = _omitFirstBeam
                        ? "\nPrvní vložka může být uložena na stěnu."
                        : "\nPole bude začínat POT nosníkem.";
                    break;
                case "vynechposledni":
                    _omitLastBeam = !_omitLastBeam;
                    msg = _omitLastBeam
                        ? "\nPoslední vložka může být uložena na stěnu."
                        : "\nPole bude končit POT nosníkem.";
                    break;
                case "ulozzacatek":
                    _beamStartPlacement = !_beamStartPlacement;
                    msg = _beamStartPlacement
                        ? "\nZačátek POT nosníku bude uložen minimálně 125 mm na stěnu."
                        : "\nZačátek POT nosníku nebude uložen.";
                    break;
                case "ulozkonec":
                    _beamEndPlacement = !_beamEndPlacement;
                    msg = _beamEndPlacement
                        ? "\nKonec POT nosníku bude uložen minimálně 125 mm na stěnu."
                        : "\nKonec POT nosníku nebude uložen.";
                    break;
            }

            if (msg != null)
            {
                Active.Editor.WriteMessage(msg);
            }
        }

        #endregion
    }
}