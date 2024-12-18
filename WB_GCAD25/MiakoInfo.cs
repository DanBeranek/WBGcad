using System.Collections.Generic;

namespace WB_GCAD25
{
    public static class MiakoData
    {
        public class MiakoInfo
        {
            public double Width_mm { get; set; }
            public double Length_mm { get; set; }
            public double Area_mm2 { get; set; }

            public double Volume_mm3
            {
                get
                {
                    return Width_mm * Area_mm2;
                }
            }
        }
        
        public static Dictionary<string, MiakoInfo> Blocks = new Dictionary<string, MiakoInfo>
        {
            {
                "MIAKO_80_500_PULENA", new MiakoInfo
                {
                    Width_mm = 500,
                    Length_mm = 125,
                    Area_mm2 = 27937.9033
                }
            },
            {
                "MIAKO_80_500", new MiakoInfo
                {
                    Width_mm = 500,
                    Length_mm = 250,
                    Area_mm2 = 27937.9033
                }
            },
            {
                "MIAKO_150_500", new MiakoInfo
                {
                    Width_mm = 500,
                    Length_mm = 250,
                    Area_mm2 = 55919.4855
                }
            }
            ,
            {
                "MIAKO_190_500", new MiakoInfo
                {
                    Width_mm = 500,
                    Length_mm = 250,
                    Area_mm2 = 72081.222
                }
            }
            ,
            {
                "MIAKO_230_500", new MiakoInfo
                {
                    Width_mm = 500,
                    Length_mm = 250,
                    Area_mm2 = 88228.8783
                }
            }
            ,
            {
                "MIAKO_250_500", new MiakoInfo
                {
                    Width_mm = 500,
                    Length_mm = 200,
                    Area_mm2 = 84130.1419
                }
            },
            {
                "MIAKO_80_625_PULENA", new MiakoInfo
                {
                    Width_mm = 625,
                    Length_mm = 125,
                    Area_mm2 = 38887.3798
                }
            },
            {
                "MIAKO_80_625", new MiakoInfo
                {
                    Width_mm = 625,
                    Length_mm = 250,
                    Area_mm2 = 38887.3798
                }
            },
            {
                "MIAKO_150_625", new MiakoInfo
                {
                    Width_mm = 625,
                    Length_mm = 250,
                    Area_mm2 = 75100.3613
                }
            }
            ,
            {
                "MIAKO_190_625", new MiakoInfo
                {
                    Width_mm = 625,
                    Length_mm = 250,
                    Area_mm2 = 96064.6955
                }
            }
            ,
            {
                "MIAKO_230_625", new MiakoInfo
                {
                    Width_mm = 625,
                    Length_mm = 250,
                    Area_mm2 = 116985.1591
                }
            }
            ,
            {
                "MIAKO_250_625", new MiakoInfo
                {
                    Width_mm = 625,
                    Length_mm = 200,
                    Area_mm2 = 114243.3363
                }
            }
        };
        
    }
}