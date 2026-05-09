using Electrons.Core.Net8.Infrastructure;
using System.Configuration;

namespace Electrons.Net8
{
    public class GameSettings
    {
        public GameSettings()
        {
            ThresholdSettings = new ThresholdSettings();
        }
        public bool ShowAppLink { get; set; }
        public bool UseApiForStandings { get; set; }
        public string ApiKey { get; set; }
        public DatabaseConfig DefaultConnection { get; set; }
        public int CurrentGameId { get; set; }
        public string JumboText { get; set; }
        public bool WhiningToggle { get; set; }
        public string CarouselImagesVirtualPath { get; set; }
        public string BaseApiUrl { get; set; }
        public ThresholdSettings ThresholdSettings { get; set; }
        public string AdminKey { get; set; }
    }
    public class ThresholdSettings
    {
        public int CareerHitting { get; set; }
        public int SeasonHitting { get; set; }
        public int PlayoffHitting { get; set; }
        public int CareerPitching { get; set; }
        public int SeasonPitching { get; set; }
        public int PlayoffPitching { get; set; }
        public string HittingThresholds => string.Format("*Min {0} AB / Season. {1} AB / Career.   {2} AB / Playoffs", SeasonHitting, CareerHitting, PlayoffHitting);
        public string PitchingThresholds => string.Format("*Min {0} IP / Season. {1} IP / Career.   {2} IP / Playoffs", SeasonPitching, CareerPitching, PlayoffPitching);
    }
}
