using Electrons.Core.Net8;

namespace Scorebook.Components;

public partial class FieldView : ContentView
{    
    public FieldView()
    {
        InitializeComponent();
    }
    public static readonly BindableProperty ViewModelProperty = BindableProperty.Create(nameof(ViewModel), typeof(ScorebookViewModel), typeof(FieldView), default(ScorebookViewModel));
    public ScorebookViewModel ViewModel
    {
        get => (ScorebookViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var paramString = e.Parameter as string ?? "0";
        var onBase = (OnBase)int.Parse(paramString);
        await ViewModel.GameCoordinator.HandleRunningEvents(onBase);
    }
    private void RbiTapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (ViewModel.CurrentRbis == 4)
            ViewModel.CurrentRbis = 0;
        else
            ViewModel.CurrentRbis++;
    }
}