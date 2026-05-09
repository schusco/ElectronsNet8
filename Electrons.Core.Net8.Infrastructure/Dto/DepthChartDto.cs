using Electrons.Core.Net8.Games;

namespace Electrons.Core.Net8.Infrastructure.Dto
{
    public class DepthChartDto
    {
        public DepthChartDto(DcPosition pos, int pid, int rank)
        {
            Position = (int)pos;
            PlayerId = pid;
            Rank = rank;
        }
        public int Position { get; set; }
        public int PlayerId { get; set; }
        public int Rank { get; set; }
    }
}
