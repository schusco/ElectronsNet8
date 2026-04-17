using CommunityToolkit.Mvvm.Messaging.Messages;
using Electrons.Core.Net8.Games;

namespace Scorebook.Messages
{
    public class PositionChangedMessage(Position value) : ValueChangedMessage<Position>(value) { }
}
