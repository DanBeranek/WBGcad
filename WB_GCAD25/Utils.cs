using System;

namespace WB_GCAD25
{
    public static class Utils
    {
        public static double RoundToBase(double x, double baseValue)
        {
            return baseValue * Math.Round(x / baseValue);
        }

        public static double CeilToBase(double x, double baseValue)
        {
            return baseValue * Math.Ceiling(x / baseValue);
        }
    }
}