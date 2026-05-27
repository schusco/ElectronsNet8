namespace Scorebook.Components;

public partial class Scoreboard : ContentView
{
    private ScorebookViewModel _vm;
    public Scoreboard()
    {
        InitializeComponent();
    }
    public Scoreboard(ScorebookViewModel vm)
    {
        _vm = vm;
        if (_vm.MobileActionTrigger)
            ScoreboardGrid.SetRow(InningDisplay, 1);
    }
}