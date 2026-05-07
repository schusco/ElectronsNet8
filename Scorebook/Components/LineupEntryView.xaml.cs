using Scorebook.ViewObjects;

namespace Scorebook.Components;

public partial class LineupEntryView : ContentView
{

	public LineupEntryView()
	{
		InitializeComponent();
	}
    public static readonly BindableProperty TeamProperty = BindableProperty.Create(nameof(Team), typeof(TeamWrapper), typeof(LineupEntryView), default(TeamWrapper));
    public TeamWrapper Team
    {
        get => (TeamWrapper)GetValue(TeamProperty);
        set => SetValue(TeamProperty, value);
    }
    private void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e) { }
}