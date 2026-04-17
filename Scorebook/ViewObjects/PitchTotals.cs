using Electrons.Core.Net8.Games;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Scorebook.ViewObjects
{
    public class PitchTotals : INotifyPropertyChanged
    {
        private PitchTotals() { }
        public PitchTotals(string name)
        {
            PlayerName = name;
        }
        public PitchTotals(PStats stats) : this(stats.PlayerName)
        {
            Balls = stats.Balls;
            Strikes = stats.Strikes;
            Total = stats.Pitches;
        }

        public string PlayerName
        {
            get => _name;
            private set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(PlayerName));
                }
            }
        }
        public int Balls
        {
            get => _balls;
            private set
            {
                if (_balls != value)
                {
                    _balls = value;
                    OnPropertyChanged(nameof(Balls));
                }
            }
        }
        public int Strikes
        {
            get => _strikes;
            private set
            {
                if (_strikes != value)
                {
                    _strikes = value;
                    OnPropertyChanged(nameof(Strikes));
                }
            }
        }
        public int Total
        {
            get => _total;
            private set
            {
                if (_total != value)
                {
                    _total = value;
                    OnPropertyChanged(nameof(Total));
                }
            }
        }
        public void Update(int balls, int strikes)
        {
            Balls = balls;
            Strikes = strikes;
            Total = strikes + balls;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public static PitchTotals Blank => new();
        private int _total;
        private int _balls;
        private int _strikes;
        private string _name = "";
    }
}
