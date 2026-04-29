
using Electrons.Core.Net8;
using Electrons.Core.Net8.Games;
using Microsoft.Maui.Layouts;


namespace Scorebook
{
    public partial class MainPage : ContentPage
    {
        private ScorebookViewModel _vm;
        private readonly string[] _runnerMenuOptions = [
            "Advance Runner",
            "Runner Advanced On Throw",
            "Move Runner Back",
            "Runner Out At Base",
            "Runner Advanced On Error",
            "Runner Out Advancing",
            "Courtesy Runner"];
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
                if (!_vm.TeamsAreLoaded)
                {
                    await _vm.LoadSchedule(1);
                    await _vm.LoadTeamsAndLeagues();                    
                    _vm.LoadRosters();
                    _vm.FilterTeams();
                }
            }
            catch (Exception ex) { }
        }
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // If the window is narrower than 800 pixels, hide the sidebar
            bool IsCompact = width < 800;

            if (IsCompact)
            {
                _vm.IsSideBarOpen = false;
                _vm.ShowDesktopActionButtons = false;
                _vm.MobileActionTrigger = true;
            }
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var isTabletLandscape = width > height;
                if (isTabletLandscape)
                {
                    MainArea.Direction = FlexDirection.Row;
                    MainArea.AlignItems = FlexAlignItems.Stretch;
                }
                else
                {
                    MainArea.Direction = FlexDirection.Column;
                    MainArea.AlignItems = FlexAlignItems.Start;
                }
                WindowsLog.IsVisible = false;
                AndroidLog.IsVisible = true;
                InningLabel.IsVisible = false;
            }
            else
            {
                WindowsLog.IsVisible = true;
                AndroidLog.IsVisible = false;
            }

        }
        private async void OnToggleSidebar(object sender, EventArgs e)
        {

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
        private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            var paramString = e.Parameter as string ?? "0";
            var onBase = (OnBase)int.Parse(paramString);
            string header = "";
            switch (onBase)
            {
                case OnBase.First:
                    header = "Runner On First";
                    break;
                case OnBase.Second:
                    header = "Runner On Second";
                    break;
                case OnBase.Third:
                    header = "Runner On Third";
                    break;
            }
            string action = await Application.Current.Windows[0].Page.DisplayActionSheet(header, "Cancel", null, _runnerMenuOptions);
            if (action == _runnerMenuOptions[0])  // "Advance Runner"
                await _vm.AdvanceRunners(onBase, AdvanceReason.Ab);
            else if (action == _runnerMenuOptions[1])  // "Runner Advanced On Throw"
                await _vm.AdvanceRunners(onBase, AdvanceReason.Throw);
            else if (action == _runnerMenuOptions[2]) // Move Runner Back
                _vm.Game.ReturnRunner(onBase);
            else if (action == _runnerMenuOptions[3]) // Runner Out At Base
            {
                _vm.ScoringIsRequired = true;
                AddRunnerOutEvent(onBase, false);
            }
            else if (action == _runnerMenuOptions[4]) // Runner Advanced On Error
                await _vm.AdvanceRunners(onBase, AdvanceReason.Error);

            else if (action == _runnerMenuOptions[5]) // Runner Out Advancing
            {
                if (!_vm.CurrentAb.Result.HasFielders)
                    _vm.ScoringIsRequired = true;
                AddRunnerOutEvent(onBase, true);
            }
            else if (action == _runnerMenuOptions[6]) // Courtesy Runner
            {
                var bench = _vm.Game.BattingTeam.Bench;
                var allRunners = bench.Select(s => s.FullName).ToArray();
                var cr = await Application.Current.Windows[0].Page.DisplayActionSheet("Select Courtesy Runner", "Cancel", null, allRunners);

                var nameNumber = cr.Split('-');
                var runner = _vm.Game.BattingTeam.GetPlayer(nameNumber[1].Split(',')[0], int.Parse(nameNumber[0]));
                var previousRunner = _vm.Game.CurrentInning.CurrentRunners[onBase];
                _vm.Game.CurrentInning.AddCourtesyRunner(runner, previousRunner);
            }
            _vm.UpdateRunners();
            //<FlyoutBase.ContextFlyout >
            //    < MenuFlyout x: Name = "RunnerMenu" >
            //        < MenuFlyoutItem Text = "Advance Runner" Command = "{Binding AdvanceRunnerCommand}" CommandParameter = "{x:Static games:AdvanceReason.Ab}" />
            //        < MenuFlyoutItem Text = "Runner Advanced On Throw" Command = "{Binding AdvanceRunnerCommand}" CommandParameter = "{x:Static games:AdvanceReason.Throw}" />
            //        < MenuFlyoutItem Text = "Move Runner Back" Command = "{Binding ReturnRunnerCommand}" />
            //        < MenuFlyoutItem Text = "Runner Out At Base" />
            //        < MenuFlyoutItem Text = "Runner Advanced On Throw" Command = "{Binding AdvanceRunnerCommand}" CommandParameter = "{x:Static games:AdvanceReason.Error}" />
            //        < MenuFlyoutItem Text = "Runner Out Advancing" />
            //        < MenuFlyoutItem Text = "Courtesy Runner" />
            //    </ MenuFlyout >
            //</ FlyoutBase.ContextFlyout >
        }

        private void RbiTapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            if (_vm.CurrentRbis == 4)
                _vm.CurrentRbis = 0;
            else
                _vm.CurrentRbis++;
        }
        private void AddRunnerOutEvent(OnBase onBase, bool advance)
        {
            var runners = _vm.Game.CurrentInning.CurrentRunners;
            if (onBase == OnBase.Third)
                _vm.Game.AddRunnerOutEvent(runners.OnThird, advance ? OnBase.None : OnBase.Third);
            if (onBase == OnBase.Second)
                _vm.Game.AddRunnerOutEvent(runners.OnSecond, advance ? OnBase.Third : OnBase.Second);
            if (onBase == OnBase.First)
                _vm.Game.AddRunnerOutEvent(runners.OnFirst, advance ? OnBase.Second : OnBase.First);
            _vm.UpdateRunners();
        }

        private void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e)
        {

        }

        private void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
        {
            Reallocate();
        }
    }
}
