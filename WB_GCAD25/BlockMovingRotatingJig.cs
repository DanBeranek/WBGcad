using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
     public class BlockMovingRotating : EntityJig
    {
        #region Fields

        public int MCurJigFactorNumber = 1;

        private Point3d _mPosition;    // Factor #1
        private double _mRotation;    // Factor #2

        #endregion

        #region Constructors

        public BlockMovingRotating(Entity ent)
            : base(ent)
        {
        }

        #endregion

        #region Overrides

        protected override bool Update()
        {
            switch (MCurJigFactorNumber)
            {
                case 1:
                    (Entity as BlockReference).Position = _mPosition;
                    break;
                case 2:
                    (Entity as BlockReference).Rotation = _mRotation;
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            switch (MCurJigFactorNumber)
            {
                case 1:
                    JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nBod vložení:");
                    PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
                    if (prResult1.Status == PromptStatus.Cancel) return SamplerStatus.Cancel;

                    if (prResult1.Value.Equals(_mPosition))
                    {
                        return SamplerStatus.NoChange;
                    }
                    else
                    {
                        _mPosition = prResult1.Value;
                        return SamplerStatus.OK;
                    }
                case 2:
                    JigPromptAngleOptions prOptions2 = new JigPromptAngleOptions("\nNatočení bloku:");
                    prOptions2.UseBasePoint = true;
                    prOptions2.BasePoint = _mPosition;
                    PromptDoubleResult prResult2 = prompts.AcquireAngle(prOptions2);
                    if (prResult2.Status == PromptStatus.Cancel) return SamplerStatus.Cancel;

                    if (prResult2.Value.Equals(_mRotation))
                    {
                        return SamplerStatus.NoChange;
                    }
                    else
                    {
                        _mRotation = prResult2.Value;
                        return SamplerStatus.OK;
                    }
                default:
                    break;
            }

            return SamplerStatus.OK;
        }

        #endregion

        #region Method to Call

        public static bool Jig(BlockReference ent)
        {
            try
            {
                Editor ed = Active.Editor;
                BlockMovingRotating jigger = new BlockMovingRotating(ent);
                PromptResult pr;
                do
                {
                    pr = ed.Drag(jigger);
                } while (pr.Status != PromptStatus.Cancel &&
                            pr.Status != PromptStatus.Error &&
                            pr.Status != PromptStatus.Keyword &&
                            jigger.MCurJigFactorNumber++ <= 2);

                return pr.Status == PromptStatus.OK;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}