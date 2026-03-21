using System;

namespace Electrons.Core.Net8
{
    public static class Utilities
    {
        public static decimal? CalculateSlg(decimal totalBases, decimal ab) => ab == 0 ? (decimal?)null : Math.Round(totalBases / ab, 3);
        public static decimal? CalculateBa(decimal h, decimal ab) => ab == 0 ? (decimal?)null : Math.Round(h / ab, 3);
        public static decimal? CalculateOps(decimal obp, decimal slg) => obp + slg;
        public static decimal? CalculateObp(decimal h, decimal bb, decimal hbp, decimal ab, decimal sf)
        {
            var pa = ab + bb + hbp + sf;
            if (pa == 0)
                return null;
            return Math.Round((h + bb + hbp) / pa, 3);
        }
        public static int CalculateTB(int h, int _2b, int _3b, int hr) => h + _2b + (2 * _3b) + (3 * hr);
        public static decimal? CalculateBB9(int bb, decimal ip) => ip > 0 ? Math.Round((decimal)bb * 7 / ip, 2) : (decimal?)null;
        public static decimal? CalculateK9(int k, decimal ip) => ip > 0 ? Math.Round((decimal)k * 7 / ip, 2) : (decimal?)null;
        public static decimal? CalculateWhip(int bb, int h, decimal ip) => ip > 0 ? Math.Round((bb + h) / ip, 2) : (decimal?)null;
        public static decimal? CalculateEra(decimal ip, int er) => ip > 0 ? Math.Round(er * 7 / ip, 2) : (decimal?)null;
    }
}
