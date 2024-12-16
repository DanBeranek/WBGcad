using System;
using System.Collections.Generic;
using System.Globalization;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
    public static class SpanPrompter
    {
        public static SpanPromptResult PromptResult()
        {
            try
            {
                // 1. Prompt for width of the room
                PromptPointResult pPtRes;
                PromptPointOptions pPtOpts = new PromptPointOptions("");
                
                // 1a: start point
                pPtOpts.Message = "\nVyberte první bod: ";
                pPtRes = Active.Editor.GetPoint(pPtOpts);
                Point3d pt1 = pPtRes.Value;
                if (pPtRes.Status != PromptStatus.OK) {
                    Active.Editor.WriteMessage("\nCommand cancelled.");
                    return null;
                }
                
                // 1b: length of the room
                pPtOpts.Message = "\nVyberte druhý bod (ve směru POT nosníků): ";
                pPtOpts.UseBasePoint = true;
                pPtOpts.BasePoint = pt1;
                pPtRes = Active.Editor.GetPoint(pPtOpts);
                Point3d pt2 = pPtRes.Value;
                if (pPtRes.Status != PromptStatus.OK) {
                    Active.Editor.WriteMessage("\nCommand cancelled.");
                    return null;
                }
                
                // 1c: width of the room
                pPtOpts.Message = "\nVyberte třetí bod (kolmo na směr POT nosníků): ";
                pPtOpts.UseBasePoint = true;
                pPtOpts.BasePoint = pt1;
                pPtRes = Active.Editor.GetPoint(pPtOpts);
                Point3d pt3 = pPtRes.Value;
                if (pPtRes.Status != PromptStatus.OK) {
                    Active.Editor.WriteMessage("\nCommand cancelled.");
                    return null;
                }
                // 2. Prompt for x_loads
                List<double> xLoads = PromptForXLoads(pt1);
                
                // 3. Prompt for max_ovn
                PromptKeywordOptions pko = new PromptKeywordOptions("\nVyberte OVN: ")
                {
                    Keywords = { "500", "500+625", "625" },
                    AllowNone = false,
                };
                pko.Keywords.Default = "625";
                PromptResult result = Active.Editor.GetKeywords(pko);
                if (result.Status != PromptStatus.OK)
                {
                    Active.Editor.WriteMessage("\nCommand cancelled.");
                    return null;
                }

                double maxOvn;
                switch (result.StringResult)
                {
                    case "500":
                        maxOvn = 500.0;
                        break;
                    case "500+625":
                        maxOvn = 562.5;
                        break;
                    case "625":
                        maxOvn = 625.0;
                        break;
                    default:
                        throw new ArgumentException($"\nWrong selection: {result.StringResult}");
                }
                
                // 4. Prompt for doubled_beams
                bool doubledBeams = PromptForYesNo("\nPoužít zdvojené nosníky? (Yes/No): ");
                // 5. Prompt for omit_first_beam
                bool omitFirstBeam = PromptForYesNo("\nMůže být první nosník vynechán? (Yes/No): ");
                // 6. Prompt for omit_last_beam
                bool omitLastBeam = PromptForYesNo("\nMůže být poslední nosník vynechán? (Yes/No): ");
                
                // 7. Prompt for beam placement
                bool beamPlacementStart = PromptForYesNo("\nPřipočítat 125 mm na začátku pro uložení? (Yes/No): ", "Yes");
                bool beamPlacementEnd = PromptForYesNo("\nPřipočítat 125 mm na konci pro uložení? (Yes/No): ", "Yes");
                
                SpanPromptResult spanResult = new SpanPromptResult
                {
                    Pt1 = pt1,
                    Pt2 = pt2,
                    Pt3 = pt3,
                    XLoads = xLoads,
                    MaxOvn = maxOvn,
                    DoubledBeams = doubledBeams,
                    OmitFirstBeam = omitFirstBeam,
                    OmitLastBeam = omitLastBeam,
                    BeamPlacementStart = beamPlacementStart,
                    BeamPlacementEnd = beamPlacementEnd
                };

                return spanResult;
            }
            catch (OperationCanceledException)
            {
                Active.Editor.WriteMessage("\nCommand cancelled by user.");
            }
            catch (ArgumentException ex)
            {
                Active.Editor.WriteMessage($"\nInput error: {ex.Message}.");
            }
            catch (System.Exception ex)
            {
                Active.Editor.WriteMessage($"\nAn unexpected error occured: {ex.Message}.");
            }

            return null;
        }
        
        private static List<double> PromptForXLoads(Point3d basePoint)
        {
            List<double> xLoads = new List<double>();
            bool continueSelecting = true;

            while (continueSelecting)
            {
                PromptPointOptions pPtOpts =
                    new PromptPointOptions(
                        "\nVyberte body, kde mají být zdvojené POT nosníky, stiskněte Enter pro ukončení zadávání: ")
                    {
                        AllowNone = true,
                        UseBasePoint = true,
                        BasePoint = basePoint,
                    };
                PromptPointResult pPtRes = Active.Editor.GetPoint(pPtOpts);

                if (pPtRes.Status == PromptStatus.OK)
                {
                    // Calculate position based on basePoint
                    Point3d pt = pPtRes.Value;
                    double length = pt.DistanceTo(basePoint);
                    xLoads.Add(length);
                    Active.Editor.WriteMessage($"\nBod přidán ve vzdálenosti {length.ToString("F2", CultureInfo.InvariantCulture)}.");
                } 
                else if (pPtRes.Status == PromptStatus.None)
                {
                    // User pressed Enter without selecting a point
                    continueSelecting = false;
                    Active.Editor.WriteMessage("\nVýběr bodů byl ukončen.");
                }
                else
                {
                    throw new OperationCanceledException("\nPříkaz zrušen.");
                }
            }
            return xLoads;
        }
        private static bool PromptForYesNo(string prompt, string defaultValue = "No")
        {
            PromptKeywordOptions options = new PromptKeywordOptions(prompt)
            {
                AllowNone = false,
            };
            options.Keywords.Add("Yes");
            options.Keywords.Add("No");
            options.Keywords.Default = defaultValue;
            
            PromptResult result = Active.Editor.GetKeywords(options);
            if (result.Status != PromptStatus.OK)
            {
                Active.Editor.WriteMessage("\nCommand cancelled.");
                return false;
            }
            return result.StringResult.ToLower().Equals("yes");
        }
    }
}