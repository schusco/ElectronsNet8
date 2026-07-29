namespace Electrons.Net8.Api.Models
{
    public class History
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Data { get; set; } = "";
        public string? YearStart { get; set; }
        public string? YearEnd { get; set; }
        public string? Finish { get; set; }
    }
}
