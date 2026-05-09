
using Scorebook.ViewObjects;

namespace Scorebook.Components;

public partial class GameSelectionView : ContentView
{
    public GameSelectionView()
    {
        InitializeComponent();
    }
    public static readonly BindableProperty SelectionProperty=BindableProperty.Create(nameof(Selection), typeof(GameSelection), typeof(GameSelectionView), default(GameSelection));

    public GameSelection Selection
    {
        get => (GameSelection)GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        bool IsSuperCompact = width > 0 && width < 500;
        if (IsSuperCompact)
        {
            GameSelectionOptions.HorizontalOptions = LayoutOptions.Center;
            GameSelectionOptions.VerticalOptions = LayoutOptions.Center;
            Grid.SetColumnSpan(GameSelectionOptions, 3);
            Grid.SetColumn(GameScheduleSelector, 1);
            Grid.SetColumnSpan(GameScheduleSelector, 2);
            Grid.SetColumn(GameConfiguration, 1);
            Grid.SetColumnSpan(GameConfiguration, 2);
        }
    }
}