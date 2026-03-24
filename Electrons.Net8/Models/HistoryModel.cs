using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Infrastructure;
using System;
using System.Collections.Generic;

namespace Electrons.Net8.Models
{
    public class HistoryModel : StatsBasedCacheModel
    {
        public HistoryModel(Repository repo,DateTime lastUpdate)
        {
            Franchise = repo.GetHistory<FranchiseHistory>();
            Stadium = repo.GetHistory<StadiumHistory>();
            Championship = repo.GetPlayoffs();
            Results = repo.GetResults();
            Manager = repo.GetManagers();
            Retired = repo.GetHistory<RetiredNumbers>();
            Players = repo.Get162Players();
            StatsLastUpdated = lastUpdate;
        }        
        public IList<FranchiseHistory> Franchise { get; set; }
        public IList<StadiumHistory> Stadium { get; set; }
        public IList<PlayoffHistory> Championship { get; set; }
        public IList<ResultsHistory> Results { get; set; }
        public IList<ManagerHistory> Manager { get; set; }
        public IList<RetiredNumbers> Retired { get; set; }
        public IList<PlayerHistory> Players { get; set; }
    }
}