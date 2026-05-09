using Electrons.Core.Net8;
using Electrons.Core.Net8.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class DepthChartModel
    {
        public DepthChartModel(Repository repo, bool admin)
        {
            CurrentYear = repo.CurrentYear;
            IsAdmin = admin;
            var dc = repo.GetDepthChart().ToDictionary(k => k.Key, v => v.Value.Select(s => s.PlayerName).ToList());
            LF = DcList.Create(DcPosition.LF, dc[DcPosition.LF]);
            RF = DcList.Create(DcPosition.RF, dc[DcPosition.RF]);
            CF = DcList.Create(DcPosition.CF, dc[DcPosition.CF]);
            _1B = DcList.Create(DcPosition._1B, dc[DcPosition._1B]);
            _2B = DcList.Create(DcPosition._2B, dc[DcPosition._2B]);
            _3B = DcList.Create(DcPosition._3B, dc[DcPosition._3B]);
            SS = DcList.Create(DcPosition.SS, dc[DcPosition.SS]);
            C = DcList.Create(DcPosition.C, dc[DcPosition.C]);
            SP = DcList.Create(DcPosition.SP, dc[DcPosition.SP]);
            RP = DcList.Create(DcPosition.RP, dc[DcPosition.RP]);
        }
        public int CurrentYear { get; set; }
        public bool IsAdmin { get; set; }
        public DcList LF { get; set; }
        public DcList CF { get; set; }
        public DcList RF { get; set; }
        public DcList _1B { get; set; }
        public DcList _2B { get; set; }
        public DcList SS { get; set; }
        public DcList _3B { get; set; }
        public DcList SP { get; set; }
        public DcList C { get; set; }
        public DcList RP { get; set; }

    }
    public class DcList
    {
        public DcPosition Position { get; set; }
        public List<string> Players { get; set; }
        public static DcList Create(DcPosition pos, List<string> dc) => new DcList { Players = dc, Position = pos };
    }
}