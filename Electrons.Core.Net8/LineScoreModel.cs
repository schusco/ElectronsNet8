using System.Collections.Generic;

namespace Electrons.Core.Net8
{
    public class LineScoreModel
    {
        public LineScoreModel()
        {
            Innings = new List<string>();
        }
        public string Team { get; set; }
        public string Runs { get; set; }
        public string Hits { get; set; }
        public string Errors { get; set; }

        public IList<string> Innings { get; set; }

    }
}
