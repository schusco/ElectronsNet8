using Electrons.Core.Net8.Games;

namespace Scorebook.ViewObjects
{
    public class StatsRow<T> where T : IHasPlayer
    {
        public StatsRow(T stat, Color rowColor)
        {
            Name = $"{stat.Player.FirstName[0]}. {stat.Player.LastName}";
            StatLine = stat;
            RowColor = rowColor;
        }
        public Color RowColor { get; set; }
        public T StatLine { get; set; }
        public string Name { get; set; }
    }
}
