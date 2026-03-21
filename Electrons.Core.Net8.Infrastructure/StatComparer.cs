using System.Collections.Generic;

namespace Electrons.Core.Net8.Infrastructure
{
    public class StatComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            var decx = decimal.Parse((x ?? 0).ToString());
            var decy = decimal.Parse((y ?? 0).ToString());
            if (decx == decy)
                return 0;
            if (decx > decy)
                return 1;
            return -1;
        }
    }
}
