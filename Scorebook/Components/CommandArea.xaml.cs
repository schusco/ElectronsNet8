namespace Scorebook.Components;

public partial class CommandArea : ContentView
{
    public event EventHandler ToggleSidebar;
    public CommandArea()
    {
        InitializeComponent();
    }    
    public HorizontalStackLayout AndroidInningLog => AndroidLog;
    private void ToggleSidebarButton_Clicked(object sender, EventArgs e)
    {
        ToggleSidebar?.Invoke(this, EventArgs.Empty);
    }
}