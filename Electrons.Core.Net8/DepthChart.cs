namespace Electrons.Core.Net8
{
    public class DepthChart
    {
        public DcPosition Position { get; internal set; }
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public int PlayerId { get; set; }
    }
}
