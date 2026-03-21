using Electrons.Core.Net8;
using Electrons.Core.Net8.Infrastructure;
using System.Collections.Generic;

namespace Electrons.Net8.Models
{
    public class DepthChartModel
    {
        public DepthChartModel(Repository repo)
        {
            CurrentYear = repo.CurrentYear;
            var dc = repo.GetDepthChart();
            LF = dc[DcPosition.LF];
            RF = dc[DcPosition.RF];
            CF = dc[DcPosition.CF];
            _1B = dc[DcPosition._1B];
            _2B = dc[DcPosition._2B];
            _3B = dc[DcPosition._3B];
            SS = dc[DcPosition.SS];
            C = dc[DcPosition.C];
            SP = dc[DcPosition.SP];
            RP = dc[DcPosition.RP];
        }
        public int CurrentYear { get; set; }
        public List<string> LF { get; set; }
        public IList<string> CF { get; set; }
        public IList<string> RF { get; set; }
        public IList<string> _1B { get; set; }
        public IList<string> _2B { get; set; }
        public IList<string> SS { get; set; }
        public IList<string> _3B { get; set; }
        public IList<string> SP { get; set; }
        public IList<string> C { get; set; }
        public IList<string> RP { get; set; }

    }
}