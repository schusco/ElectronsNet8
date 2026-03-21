using Electrons.Core.Net8.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Electrons.Net8.Models
{
    public class RosterModel
    {
        public RosterModel(Repository repo)
        {
            Roster = [.. repo.GetRoster(true).Select(PlayerModel.Create)];
        }

        public List<PlayerModel> Roster { get; set; }
    }
}