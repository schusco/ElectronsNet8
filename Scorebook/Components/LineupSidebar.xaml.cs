using Scorebook.ViewObjects;

namespace Scorebook.Components;

public partial class LineupSidebar : ContentView
{
    public LineupSidebar()
    {
        InitializeComponent();
    }
    public static readonly BindableProperty ViewModelProperty = BindableProperty.Create(nameof(ViewModel), typeof(TeamWrapper), typeof(LineupSidebar), default(TeamWrapper), propertyChanged: OnViewModelChanged);

    public TeamWrapper ViewModel
    {
        get => (TeamWrapper)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is LineupSidebar sidebar && newValue is TeamWrapper vm)
        {
          //  sidebar.BindingContext = vm;
        }
    }
}