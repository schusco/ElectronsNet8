using CommunityToolkit.Mvvm.Messaging;
using Electrons.Core.Net8.Games;
using Scorebook.Messages;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Scorebook.ViewObjects
{
    public class LineupPosition : INotifyPropertyChanged
    {
        public LineupPosition(Player player, int spot)
        {
            _player = player;
            LineupNumber = spot;
            if (player.Position != null)
                Position = player.Position;
            if (player.IsUnknown)
                CanReplace = true;
        }
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }
        public int LineupNumber
        {
            get => _lineupNumber;
            set
            {
                if (_lineupNumber != value)
                {
                    _lineupNumber = value;
                    OnPropertyChanged(nameof(LineupNumber));
                }
            }
        }
        public bool IsConflict
        {
            get => _isConflict;
            set
            {
                if (_isConflict != value)
                {
                    _isConflict = value;
                    OnPropertyChanged(nameof(IsConflict));
                }
            }
        }
        public bool CanReplace
        {
            get => _canReplace; 
            set
            {
                _canReplace = value;
                OnPropertyChanged(nameof(CanReplace));
            }
        }
        public Player Player
        {
            get => _player;
            set
            {
                if (CanReplace)
                {
                    _player = value;
                    OnPropertyChanged(nameof(Player));
                    CanReplace = false;
                }
            }
        }
        public Position Position
        {
            get => _position;
            set
            {
                if (_position == value || value == null) return;

                _position = value;
                OnPropertyChanged(nameof(Position));
                Player.SetPosition(_position);
                WeakReferenceMessenger.Default.Send(new PositionChangedMessage(_position));
            }
        }
        public static List<Position> Positions => [.. Position.All];
        public bool HasDH => HittingFor is not null;
        public string HittingForText => HasDH ? $"{HittingFor.Position.PositionString} - {HittingFor.LastName}" : "";
        public Player HittingFor
        {
            get => _hittingFor;
            internal set
            {
                _hittingFor = value;
                Player.SetDhFor(value);
                OnPropertyChanged(nameof(HittingFor));
                OnPropertyChanged(nameof(HasDH));
                OnPropertyChanged(nameof(HittingForText));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private int _lineupNumber;
        private Position _position = Position.EH;
        private bool _isConflict = false;
        private bool _isActive = false;
        private Player? _hittingFor;
        private bool _canReplace;
        private Player _player;
        private void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
