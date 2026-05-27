using Scorebook.Services;
using Scorebook.Components;

namespace Scorebook
{
    public partial class MainPage : ContentPage
    {
        private ScorebookViewModel _vm;

        public MainPage(ScorebookViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }
        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                if (!ScorebookViewModel.TeamsAreLoaded)
                {
                    await _vm.LoadSchedule(1);
                    await _vm.LoadTeamsAndLeagues();
                    ApiService.LoadRosters();
                    _vm.GameSelection.FilterTeams();
                }
            }
            catch (Exception ex) { }
        }
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // If the window is narrower than 800 pixels, hide the sidebar
            bool IsCompact = width < 900;

            if (IsCompact)
            {
                _vm.IsSideBarOpen = false;
                _vm.ShowDesktopActionButtons = false;
                _vm.MobileActionTrigger = true;
            }
            else
            {
                _vm.ShowDesktopActionButtons = true;
                _vm.MobileActionTrigger = false;
            }
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                _vm.MobileActionTrigger = true;
                _vm.ShowDesktopActionButtons = false;
                var isTabletLandscape = width > height;
                CommandArea.HorizontalOptions = LayoutOptions.Fill;
                FieldArea.HorizontalOptions = LayoutOptions.Fill;
                var displayWidth = width - 10;
                var maxHeight = 500;
                var displayHeight = height > maxHeight ? maxHeight : height;
                CommandArea.Padding = new Thickness(5, 10, 0, 0);
                if (isTabletLandscape)
                {
                    MainArea.Padding = new Thickness(0, 0, 0, 0);
                    MainArea.SetRow(CommandArea, 0);
                    MainArea.SetColumn(CommandArea, 1);
                    Row1.Height = 0;
                    Row0.Height = new GridLength(1, GridUnitType.Star);
                    Col0.Width = new GridLength(1, GridUnitType.Star);
                    Col1.Width = new GridLength(1, GridUnitType.Star);
                    FieldArea.WidthRequest = displayWidth / 2;
                    CommandArea.WidthRequest = displayWidth / 2;
                    CommandArea.TranslationX = 10;
                    FieldArea.HeightRequest = maxHeight;
                    CommandArea.HeightRequest = maxHeight;
                }
                else
                {

                    MainArea.SetRow(CommandArea, 1);
                    MainArea.SetColumn(CommandArea, 0);
                    Row1.Height = new GridLength(1, GridUnitType.Auto);
                    Row0.Height = new GridLength(1, GridUnitType.Auto);
                    Col1.Width = 0;
                    CommandArea.TranslationX = 0;
                    CommandArea.WidthRequest = displayWidth;
                    FieldArea.WidthRequest = displayWidth;
                    FieldArea.VerticalOptions = LayoutOptions.Start;
                    FieldArea.HeightRequest = height / 2;
                    CommandArea.HeightRequest = height / 2;
                }
                Header.WidthRequest = displayWidth;
                WindowsLog.IsVisible = false;
                CommandArea.AndroidInningLog.IsVisible = true;
                InningLabel.IsVisible = false;
            }
            else
            {
                MainArea.SetRow(CommandArea, 1);
                MainArea.SetColumn(CommandArea, 0);
                Col1.Width = 0;
                WindowsLog.IsVisible = true;
                CommandArea.AndroidInningLog.IsVisible = false;
            }
            Reallocate();
        }
        private void Reallocate()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                if (_vm.IsSideBarOpen)
                {
                    SideBarColumnLeft.Width = new GridLength(2, GridUnitType.Star);
                    SideBarColumnRight.Width = new GridLength(2, GridUnitType.Star);
                }
                else
                {
                    SideBarColumnLeft.Width = new GridLength(0);
                    SideBarColumnRight.Width = new GridLength(0);
                }
            }
        }
        private void OnToggleSidebar(object sender, EventArgs e)
        {
            Reallocate();
        }
        private void SideBarToggle_Tapped(object sender, TappedEventArgs e)
        {
            Reallocate();
        }        
    }
}
