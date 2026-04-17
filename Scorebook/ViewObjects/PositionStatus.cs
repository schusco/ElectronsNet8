using CommunityToolkit.Mvvm.ComponentModel;

namespace Scorebook.ViewObjects
{
    public class PositionStatus : ObservableObject
    {
        public string PositionString { get; set; } = "";
        public Color StatusColor { get; set; } = Colors.Green; // Blue = Empty, Green = OK, Red = Conflict
    }
}
