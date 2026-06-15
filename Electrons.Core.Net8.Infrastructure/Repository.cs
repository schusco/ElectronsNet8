using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Games;
using Electrons.Core.Net8.Infrastructure.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Electrons.Core.Net8.Infrastructure
{
    public interface IRepository
    {
        Task<IList<HittingStatsRow>> GetSeasonHittingStatsAsync(int year, bool playoffs = false, DateTime? toDate = null);
        Task<IList<PitchingStatsRow>> GetSeasonPitchingStatsAsync(int year, bool playoffs = false);
        Task<DateTime> GetStatsLastUpdatedAsync();
        //Task<IList<HittingStatsRow>> GetCareerHittingStatsAsync();
        // Task<IList<PitchingStatsRow>> GetCareerPitchingStatsAsync();
    }
    public class Repository : IRepository
    {
        private readonly ISession _session;
        private readonly IMemoryCache _cache;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly object _lock = new object();
        public Repository(ISession session, IMemoryCache cache)
        {
            _session = session;
            _cache = cache;
        }
        public Repository(DatabaseConfig config)
        {
            var helper = new NHibernateHelper(config);
            _session = helper.OpenSession(false);
        }
        public ISession Session { get => _session; }
        public int CurrentYear => Session.QueryOver<GameData>().SelectList(s => s.SelectMax(q => q.GameDate)).SingleOrDefault<DateTime>().Year;
        //public bool IsLiveGameInProgress
        //{
        //    get
        //    {
        //        var game = CurrentLiveGame;
        //        return game.IsStarted && !game.IsGameOver;
        //    }
        //}
        //public BaseballGame CurrentLiveGame => Session.Get<GameData>(CurrentGameId).FullGame;
        //private int CurrentGameId { get; set; }
        public IEnumerable<StandingsRow> GetStandings(string division = "")
        {
            var query = Session.QueryOver<StandingsRow>().Where(w => w.IsActive).OrderBy(o => o.Points).Desc.ThenBy(t => t.Losses).Asc.ThenBy(t => t.Wins).Desc;
            if (!string.IsNullOrEmpty(division))
                query = query.Where(w => w.Division == division);
            return query.List();
        }
        public IList<PlayerHistory> Get162Players()
        {
            return Session.QueryOver<PlayerHistory>().List().OrderBy(o => o.Date).ToList();
        }
        public IList<BirthdayModel> GetBirthdays(int month)
        {
            PlayerProfile player = null;
            BirthdayModel model = null;
            return Session.QueryOver(() => player).Where(w => w.Current)
                .SelectList(s => s.Select(() => player.DOB).WithAlias(() => model.BirthDate)
                .Select(() => player.LastName).WithAlias(() => model.LastName)
                .Select(() => player.FirstName).WithAlias(() => model.FirstName))
                .TransformUsing(Transformers.AliasToBean<BirthdayModel>()).List<BirthdayModel>().Where(w => w.BirthDate.Month == month).ToList();
        }
        public IList<PlayerProfile> GetCurrentPlayers()
        {
            return Session.QueryOver<PlayerProfile>().Where(w => w.Current).List();
        }
        public IList<Location> GetCurrentLocations()
        {
            return Session.QueryOver<Location>().Where(w => w.Current).List();
        }
        public IList<ScheduleEvent> GetAllEvents()
        {
            return Session.QueryOver<ScheduleEvent>().List();
        }
        public IList<LeadersRow> GetPitchingLeaders(StatsCategory stat, PitchingCategories category)
        {
            var propName = category.GetPropertyName();
            var displayName = category.GetPropertyDisplayName();
            LeadersRow model = null;
            PlayerProfile player = null;
            GameData game = null;
            int mininnings;
            var query = Session.QueryOver<PitchingStats>().JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Player, () => player);
            if (stat == StatsCategory.Season)
            {
                query = query.SelectList(x => x.Select(Projections.GroupProperty(NhProjections.Year(() => game.GameDate)).WithAlias(() => model.Season))
                            .SelectGroup(() => player.FirstName).WithAlias(() => model.FirstName)
                            .SelectGroup(() => player.LastName).WithAlias(() => model.LastName)
                            .Select(NhProjections.Constant(displayName)).WithAlias(() => model.Header)
                            .Select(Projections.Sum(propName)).WithAlias(() => model.Stat));
                mininnings = 25;
            }
            else
            {
                query = query.SelectList(x => x.SelectGroup(() => player.FirstName).WithAlias(() => model.FirstName)
                            .SelectGroup(() => player.LastName).WithAlias(() => model.LastName)
                            .Select(NhProjections.Constant(displayName)).WithAlias(() => model.Header)
                            .Select(Projections.Sum(propName)).WithAlias(() => model.Stat));
                mininnings = 50;
            }
            var sortAscending = category.GetStatSort();
            query = query.Where(Restrictions.Ge(Projections.Sum<PitchingStats>(p => p.InningsPitched), mininnings))
                        .TransformUsing(Transformers.AliasToBean<LeadersRow>());
            query = sortAscending ? query.OrderBy(Projections.Sum(propName)).Asc : query.OrderBy(Projections.Sum(propName)).Desc;
            return query.Take(15).List<LeadersRow>();
        }
        public IList<LeadersRow> GetHittingLeaders(StatsCategory stat, HittingCategories category)
        {
            var propName = category.GetPropertyName();
            var displayName = category.GetPropertyDisplayName();
            LeadersRow model = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<HittingStats>().JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Profile, () => player);
            if (stat == StatsCategory.Season)
                query = query.SelectList(x => x.Select(Projections.GroupProperty(NhProjections.Year(() => game.GameDate)).WithAlias(() => model.Season))
                            .SelectGroup(() => player.FirstName).WithAlias(() => model.FirstName)
                            .SelectGroup(() => player.LastName).WithAlias(() => model.LastName)
                            .Select(NhProjections.Constant(displayName)).WithAlias(() => model.Header)
                            .Select(Projections.Sum(propName)).WithAlias(() => model.Stat));
            else
                query = query.SelectList(x => x.SelectGroup(() => player.FirstName).WithAlias(() => model.FirstName)
                            .SelectGroup(() => player.LastName).WithAlias(() => model.LastName)
                            .Select(NhProjections.Constant(displayName)).WithAlias(() => model.Header)
                            .Select(Projections.Sum(propName)).WithAlias(() => model.Stat));

            return query.TransformUsing(Transformers.AliasToBean<LeadersRow>()).OrderBy(Projections.Sum(propName)).Desc.Take(15).List<LeadersRow>();
        }
        public GameData GetNextOuting(DateTime time) => Session.QueryOver<GameData>().Where(w => w.GameDate > time).Take(1).SingleOrDefault();
        public IList<RosterRow> GetRoster(bool current)
        {
            RosterRow row = null;
            PlayerProfile player = null;
            GameData game = null;
            var subquery = QueryOver.Of<HittingStats>().JoinAlias(j => j.Game, () => game).Where(w => w.Profile.Id == player.Id)
                     .Select(Projections.Min(NhProjections.Year(() => game.GameDate)));
            return Session.QueryOver(() => player)
                    .SelectList(s => s.SelectSubQuery(subquery).WithAlias(() => row.RookieYear)
                    .Select(() => player.Id).WithAlias(() => row.Id)
                    .Select(() => player.LastName).WithAlias(() => row.LastName)
                    .Select(() => player.FirstName).WithAlias(() => row.FirstName)
                    .Select(() => player.Nickname).WithAlias(() => row.Nickname)
                    .Select(() => player.Height).WithAlias(() => row.Height)
                    .Select(() => player.Weight).WithAlias(() => row.Weight)
                    .Select(() => player.Hometown).WithAlias(() => row.BirthPlace)
                    .Select(() => player.POS1).WithAlias(() => row.Pos1)
                    .Select(() => player.POS2).WithAlias(() => row.Pos2)
                    .Select(() => player.POS3).WithAlias(() => row.Pos3)
                    .Select(() => player.Email).WithAlias(() => row.Email)
                    .Select(() => player.UniformNumber).WithAlias(() => row.Number))
                    .Where(w => w.Current == current).Where(w => !w.IsHidden)
                    .TransformUsing(Transformers.AliasToBean<RosterRow>()).OrderBy(o => o.UniformNumber).Asc
                    .List<RosterRow>();
        }
        public PlayerProfile GetManager()
        {
            var managerId = Session.QueryOver<ManagerHistory>().Where(w => w.YearStart == DateTime.Now.Year.ToString()).SingleOrDefault()?.Data;
            return Session.Load<PlayerProfile>(int.Parse(managerId));
        }
        public bool AddEvent(DateTime evDate, string evText)
        {
            return WrapInTryCatch(() =>
            {
                var ev = ScheduleEvent.CreateNew(evDate, evText);
                Session.Save(ev);
            });
        }
        public PlayerProfile GetPlayer(int pid) => Session.Get<PlayerProfile>(pid);
        public PlayerProfile GetPlayerByName(string lastName, string firstName)
        {
            var players = Session.QueryOver<PlayerProfile>().Where(w => w.LastName == lastName).List();
            if (players.Count == 1)
                return players.Single();
            return players.SingleOrDefault(w => w.FirstName == firstName);
        }
        public PlayerProfile GetPlayerByUniformNumber(int number)
        {
            var player = Session.QueryOver<PlayerProfile>().Where(w => w.UniformNumber == number && w.Current)
                .SingleOrDefault();

            return player;
        }
        public bool AddHittingStats(GameData game, HittingStatsRow stats)
        {
            return WrapInTryCatch(() =>
            {
                var player = Session.Load<PlayerProfile>(stats.Id);
                game.AddHittingStats(stats, player);
                Session.SaveOrUpdate(game);
            });
        }
        private void AddHittingTotals(IList<HittingStatsRow> list)
        {
            var firstRow = list.First();
            firstRow.SumAb = list.Sum(s => s.AtBats);
            firstRow.SumR = list.Sum(s => s.Runs);
            firstRow.SumH = list.Sum(s => s.Hits);
            firstRow.SumHr = list.Sum(s => s.HomeRuns);
            firstRow.SumRbi = list.Sum(s => s.Rbis);
            firstRow.SumBb = list.Sum(s => s.Walks) ?? 0;
            firstRow.SumK = list.Sum(s => s.StrikeOuts);
            firstRow.SumSb = list.Sum(s => s.StolenBases);
            firstRow.TotalBa = Utilities.CalculateBa(firstRow.SumH, firstRow.SumAb);
            firstRow.TotalSlg = Utilities.CalculateSlg(list.Sum(s => s.TotalBases), list.Sum(s => s.AtBats));
            firstRow.TotalObp = Utilities.CalculateObp(list.Sum(s => s.Hits), list.Sum(s => s.Walks ?? 0), list.Sum(s => s.Hbp ?? 0), list.Sum(s => s.AtBats), list.Sum(s => s.SacFlies ?? 0));
            firstRow.TotalOps = firstRow.TotalSlg + firstRow.TotalObp;
            firstRow.SumLob = list.Sum(s => s.LeftOnBase);
            firstRow.SumSf = list.Sum(s => s.SacFlies);
            firstRow.SumSac = list.Sum(s => s.SacBunts);
            firstRow.SumCs = list.Sum(s => s.CaughtStealing);
            firstRow.SumHbp = list.Sum(s => s.Hbp);
            firstRow.Sum3b = list.Sum(s => s.Triples);
            firstRow.Sum2b = list.Sum(s => s.Doubles);
            firstRow.SumG = list.Sum(s => s.Games ?? 0);
        }
        public bool AddPitchingStats(GameData game, PitchingStatsRow stats, string dec)
        {
            return WrapInTryCatch(() =>
            {
                var player = Session.Load<PlayerProfile>(stats.Id);
                game.AddPitchingStats(stats, player, dec);
                Session.SaveOrUpdate(game);
            });
        }
        private void AddPitchingTotals(IList<PitchingStatsRow> list)
        {
            var firstRow = list.First();
            firstRow.SumW = list.Sum(s => s.Wins);
            firstRow.SumL = list.Sum(s => s.Losses);
            firstRow.SumS = list.Sum(s => s.Saves);
            firstRow.SumSvo = list.Sum(s => s.SaveOpportunities);
            firstRow.SumG = list.Sum(s => s.Games);
            firstRow.SumGs = list.Sum(s => s.Starts) ?? 0;
            decimal totalOuts = 0;
            foreach (var row in list)
            {
                decimal start = row.Innings;
                decimal outs = start * 3;
                decimal total = Math.Round(outs);
                totalOuts += total;
            }
            decimal totalInnings = totalOuts / 3;
            firstRow.SumIp = decimal.Parse(string.Format("{0}.{1}", Math.Floor(totalOuts / 3), totalOuts % 3));
            firstRow.SumH = list.Sum(s => s.Hits);
            firstRow.SumR = list.Sum(s => s.Runs);
            firstRow.SumEr = list.Sum(s => s.EarnedRuns);
            firstRow.SumBb = list.Sum(s => s.Walks);
            firstRow.SumK = list.Sum(s => s.StrikeOuts);
            firstRow.SumHb = list.Sum(s => s.HitBatters);
            firstRow.SumHr = list.Sum(s => s.HomeRuns);
            firstRow.SumBf = list.Sum(s => s.BattersFaced);
            firstRow.SumCg = list.Sum(s => s.CompleteGames);
            firstRow.TotalEra = Utilities.CalculateEra(totalInnings, firstRow.SumEr ?? 0);
            if (totalInnings != 0)
            {
                firstRow.TotalWhip = Utilities.CalculateWhip(firstRow.SumBb ?? 0, firstRow.SumH ?? 0, totalInnings);
                firstRow.TotalK9 = Utilities.CalculateK9(firstRow.SumK ?? 0, totalInnings);
                firstRow.TotalBB9 = Utilities.CalculateBB9(firstRow.SumBb ?? 0, totalInnings);
            }
        }
        public bool UpdateInning(GameData game, int? topR, int? botR, int? topH, int? botH, int? topE, int? botE, int inning)
        {
            return WrapInTryCatch(() =>
            {
                game.UpdateInning(inning, topR, botR, topH, botH, topE, botE);
                Session.SaveOrUpdate(game);
            });
        }
        private async Task<IList<HittingStatsRow>> GetCareerHittingStatsAsync()
        {
            HittingStatsRow model = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<HittingStats>()
                .JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Profile, () => player).SelectList(x => x.SelectSum(s => s.AtBats).WithAlias(() => model.AtBats)
                .SelectMax(() => player.FirstName).WithAlias(() => model.FirstName)
                .SelectMax(() => player.LastName).WithAlias(() => model.LastName)
                .SelectSum(s => s.Runs).WithAlias(() => model.Runs)
                .SelectSum(s => s.Hits).WithAlias(() => model.Hits)
                .SelectSum(s => s.Doubles).WithAlias(() => model.Doubles)
                .SelectSum(s => s.Triples).WithAlias(() => model.Triples)
                .SelectSum(s => s.HomeRuns).WithAlias(() => model.HomeRuns)
                .SelectSum(s => s.RunsBattedIn).WithAlias(() => model.Rbis)
                .SelectSum(s => s.Walks).WithAlias(() => model.Walks)
                .SelectSum(s => s.HitByPitches).WithAlias(() => model.Hbp)
                .SelectSum(s => s.StrikeOuts).WithAlias(() => model.StrikeOuts)
                .SelectSum(s => s.StolenBases).WithAlias(() => model.StolenBases)
                .SelectSum(s => s.CaughtStealing).WithAlias(() => model.CaughtStealing)
                .SelectSum(s => s.SacrificeBunts).WithAlias(() => model.SacBunts)
                .SelectSum(s => s.SacFlies).WithAlias(() => model.SacFlies)
                .SelectSum(s => s.LeftOnBase).WithAlias(() => model.LeftOnBase)
                .SelectGroup(() => player.Id).WithAlias(() => model.Id)
                .SelectGroup(() => game.Playoff).WithAlias(() => model.Playoff)
                .Select(Projections.GroupProperty(NhProjections.Year(() => game.GameDate)).WithAlias(() => model.Year))
                                ).Where(() => !player.IsHidden)
                                .TransformUsing(Transformers.AliasToBean<HittingStatsRow>());

            var list = await query.ListAsync<HittingStatsRow>();
            //var 
            //  .Result.ToList();
            //                .OrderBy(o => o.Year).ThenBy(o => o.Playoff).ToList();
            //list.ForEach(f => f.DisplayAll(true));
            return list;
        }
        public async Task<IList<PitchingStatsRow>> GetCareerPitchingStatsAsync()
        {
            PitchingStatsRow row = null;
            GameData subGame = null;
            GameData game = null;
            PlayerProfile player = null;

            var winsSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => w.DecisionVal == "W" || w.DecisionVal == "BS,W").Where(w => w.Player.Id == player.Id && game.Playoff == subGame.Playoff)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate)));

            var lossesSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => w.DecisionVal == "L" || w.DecisionVal == "BS,L").Where(w => w.Player.Id == player.Id && game.Playoff == subGame.Playoff)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate)));

            var savesSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => w.DecisionVal == "S").Where(w => w.Player.Id == player.Id && game.Playoff == subGame.Playoff)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate)));

            var bsSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => w.DecisionVal == "S" || w.DecisionVal == "BS" || w.DecisionVal == "BS,W" || w.DecisionVal == "BS,L").Where(w => w.Player.Id == player.Id && game.Playoff == subGame.Playoff)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate)));

            var list = await Session.QueryOver<PitchingStats>()
                .JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Player, () => player)
                .SelectList(z => z.SelectCount(s => s.Id).WithAlias(() => row.Games)
                 .SelectMax(() => player.FirstName).WithAlias(() => row.FirstName)
                 .SelectMax(() => player.LastName).WithAlias(() => row.LastName)
                 .SelectSubQuery(winsSubquery).WithAlias(() => row.Wins)
                 .SelectSubQuery(lossesSubquery).WithAlias(() => row.Losses)
                 .SelectSubQuery(savesSubquery).WithAlias(() => row.Saves)
                 .SelectSubQuery(bsSubquery).WithAlias(() => row.SaveOpportunities)
                 .SelectSum(s => s.GameStarted).WithAlias(() => row.Starts)
                 .SelectSum(s => s.InningsPitched).WithAlias(() => row.Innings)
                 .SelectSum(s => s.BattersFaced).WithAlias(() => row.BattersFaced)
                 .SelectSum(s => s.Hits).WithAlias(() => row.Hits)
                 .SelectSum(s => s.Runs).WithAlias(() => row.Runs)
                 .SelectSum(s => s.EarnedRuns).WithAlias(() => row.EarnedRuns)
                 .SelectSum(s => s.Walks).WithAlias(() => row.Walks)
                 .SelectSum(s => s.HitBatters).WithAlias(() => row.HitBatters)
                 .SelectSum(s => s.HomeRuns).WithAlias(() => row.HomeRuns)
                 .SelectSum(s => s.StrikeOuts).WithAlias(() => row.StrikeOuts)
                 .SelectSum(s => s.CompleteGames).WithAlias(() => row.CompleteGames)
                 .SelectGroup(() => player.Id).WithAlias(() => row.Id)
                 .SelectGroup(() => game.Playoff).WithAlias(() => row.Playoff)
                 .Select(Projections.GroupProperty(NhProjections.Year(() => game.GameDate)).WithAlias(() => row.Year))
                 ).Where(() => !player.IsHidden)
                 .TransformUsing(Transformers.AliasToBean<PitchingStatsRow>()).ListAsync<PitchingStatsRow>();

            // list.ForEach(f => f.DisplayAll(true));
            return list;
        }
        public IList<HittingStatsRow> GetCareerHittingStats(int pid)
        {
            HittingStatsRow model = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<HittingStats>()
                .JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Profile, () => player);
            query = query.Where(w => w.Profile.Id == pid).TransformUsing(Transformers.AliasToBean<HittingStatsRow>());
            query = GetHittingSelectList(query, Projections.GroupProperty(NhProjections.Year(() => game.GameDate)), () => model.Year);

            var list = query.List<HittingStatsRow>()
                .OrderBy(o => o.Year).ThenBy(o => o.Playoff).ToList();

            if (list.Any())
                AddHittingTotals(list);
            list.ForEach(f => f.Player = Player.Create(f.UniformNumber, f.FirstName, f.LastName));
            return list;
        }
        public IList<PitchingStatsRow> GetCareerPitchingStats(int pid)
        {
            PitchingStatsRow row = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<PitchingStats>()
                .JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Player, () => player).Where(w => w.Player.Id == pid);
            query = GetPitchingSelectList(query, pid, true, () => row.Year);
            var list = query.TransformUsing(Transformers.AliasToBean<PitchingStatsRow>()).List<PitchingStatsRow>();

            if (list.Any())
                AddPitchingTotals(list);

            return list;
        }
        private IQueryOver<HittingStats, HittingStats> GetHittingSelectList(IQueryOver<HittingStats, HittingStats> query, IProjection grouping, Expression<Func<object>> alias)
        {
            HittingStatsRow model = null;
            PlayerProfile player = null;
            GameData game = null;

            query = query.SelectList(x => x.SelectSum(s => s.AtBats).WithAlias(() => model.AtBats)
            .SelectCount(() => game.GameId).WithAlias(() => model.Games)
                .SelectMax(() => player.FirstName).WithAlias(() => model.FirstName)
                .SelectMax(() => player.LastName).WithAlias(() => model.LastName)
                .SelectMax(() => player.UniformNumber).WithAlias(() => model.UniformNumber)
                .SelectSum(s => s.Runs).WithAlias(() => model.Runs)
                .SelectSum(s => s.Hits).WithAlias(() => model.Hits)
                .SelectSum(s => s.Doubles).WithAlias(() => model.Doubles)
                .SelectSum(s => s.Triples).WithAlias(() => model.Triples)
                .SelectSum(s => s.HomeRuns).WithAlias(() => model.HomeRuns)
                .SelectSum(s => s.RunsBattedIn).WithAlias(() => model.Rbis)
                .SelectSum(s => s.Walks).WithAlias(() => model.Walks)
                .SelectSum(s => s.HitByPitches).WithAlias(() => model.Hbp)
                .SelectSum(s => s.StrikeOuts).WithAlias(() => model.StrikeOuts)
                .SelectSum(s => s.StolenBases).WithAlias(() => model.StolenBases)
                .SelectSum(s => s.CaughtStealing).WithAlias(() => model.CaughtStealing)
                .SelectSum(s => s.SacrificeBunts).WithAlias(() => model.SacBunts)
                .SelectSum(s => s.SacFlies).WithAlias(() => model.SacFlies)
                .SelectSum(s => s.LeftOnBase).WithAlias(() => model.LeftOnBase)
                .SelectGroup(() => game.Playoff).WithAlias(() => model.Playoff)
                .SelectCount(() => game.GameId).WithAlias(() => model.Games)
                .Select(grouping).WithAlias(alias))
                .TransformUsing(Transformers.AliasToBean<HittingStatsRow>());

            return query;
        }
        private IQueryOver<PitchingStats, PitchingStats> GetPitchingSelectList(IQueryOver<PitchingStats, PitchingStats> query, int restriction, bool career, Expression<Func<object>> alias)
        {
            PitchingStatsRow row = null;
            GameData subGame = null;
            GameData game = null;
            PlayerProfile player = null;

            var winsSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => game.Playoff == subGame.Playoff && (w.DecisionVal == "W" || w.DecisionVal == "BS,W"));

            winsSubquery = career ? winsSubquery.Where(w => w.Player.Id == restriction)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate))) :
                winsSubquery = winsSubquery.Where(Restrictions.Eq(NhProjections.Year(() => subGame.GameDate), restriction)).Where(w => w.Player.Id == player.Id);

            var lossesSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => game.Playoff == subGame.Playoff && (w.DecisionVal == "L" || w.DecisionVal == "BS,L"));

            lossesSubquery = career ? lossesSubquery.Where(w => w.Player.Id == restriction)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate))) :
                lossesSubquery.Where(Restrictions.Eq(NhProjections.Year(() => subGame.GameDate), restriction)).Where(w => w.Player.Id == player.Id);

            var savesSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => game.Playoff == subGame.Playoff && w.DecisionVal == "S");

            savesSubquery = career ? savesSubquery.Where(w => w.Player.Id == restriction)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate))) :
                savesSubquery.Where(Restrictions.Eq(NhProjections.Year(() => subGame.GameDate), restriction)).Where(w => w.Player.Id == player.Id);

            var bsSubquery = QueryOver.Of<PitchingStats>().JoinAlias(p => p.Game, () => subGame).SelectList(s => s.SelectCount(p => p.Id))
                .Where(w => game.Playoff == subGame.Playoff && (w.DecisionVal == "S" || w.DecisionVal == "BS" || w.DecisionVal == "BS,W" || w.DecisionVal == "BS,L"));

            bsSubquery = career ? bsSubquery.Where(w => w.Player.Id == restriction)
                .Where(Restrictions.EqProperty(NhProjections.Year(() => game.GameDate), NhProjections.Year(() => subGame.GameDate))) :
                bsSubquery.Where(Restrictions.Eq(NhProjections.Year(() => subGame.GameDate), restriction)).Where(w => w.Player.Id == player.Id);

            var grouping = career ? Projections.GroupProperty(NhProjections.Year(() => game.GameDate)) :
                Projections.GroupProperty(Projections.Property(() => player.Id));

            return query.SelectList(z => z.Select(grouping).WithAlias(alias)
                 .SelectCount(s => s.Id).WithAlias(() => row.Games)
                 .SelectMax(() => player.FirstName).WithAlias(() => row.FirstName)
                 .SelectMax(() => player.LastName).WithAlias(() => row.LastName)
                 .SelectSubQuery(winsSubquery).WithAlias(() => row.Wins)
                 .SelectSubQuery(lossesSubquery).WithAlias(() => row.Losses)
                 .SelectSubQuery(savesSubquery).WithAlias(() => row.Saves)
                 .SelectSubQuery(bsSubquery).WithAlias(() => row.SaveOpportunities)
                 .SelectSum(s => s.GameStarted).WithAlias(() => row.Starts)
                 .SelectSum(s => s.InningsPitched).WithAlias(() => row.Innings)
                 .SelectSum(s => s.BattersFaced).WithAlias(() => row.BattersFaced)
                 .SelectSum(s => s.Hits).WithAlias(() => row.Hits)
                 .SelectSum(s => s.Runs).WithAlias(() => row.Runs)
                 .SelectSum(s => s.EarnedRuns).WithAlias(() => row.EarnedRuns)
                 .SelectSum(s => s.Walks).WithAlias(() => row.Walks)
                 .SelectSum(s => s.HitBatters).WithAlias(() => row.HitBatters)
                 .SelectSum(s => s.HomeRuns).WithAlias(() => row.HomeRuns)
                 .SelectSum(s => s.StrikeOuts).WithAlias(() => row.StrikeOuts)
                 .SelectSum(s => s.CompleteGames).WithAlias(() => row.CompleteGames)
                 .SelectGroup(() => game.Playoff).WithAlias(() => row.Playoff))
                 .TransformUsing(Transformers.AliasToBean<PitchingStatsRow>());
        }
        public bool UpdatePlayer(PlayerProfile pp) => WrapInTryCatch(() => Session.SaveOrUpdate(pp));
        public bool Save<T>(T entity) => WrapInTryCatch(() => Session.Save(entity));
        public GameData GetGameById(int gid)
        {
            using (var trx = Session.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var gd = Session.Get<GameData>(gid);
                return gd;
            }
        }
        public GameData GetGameByDate(DateTime gameDate, string opponent = null)
        {
            GameData gd = null;
            var gm = Session.QueryOver(() => gd)
                .Where(Restrictions.Eq(NhProjections.Year(() => gd.GameDate), gameDate.Year));
            if (!string.IsNullOrEmpty(opponent))
                gm = gm.Where(w => w.Opponent == opponent);
            var g = gm.List();
            if (g.Count == 1)
                return g.First();
            else if (g.Count(a => a.GameDate.Date == gameDate.Date) == 1)
                return g.Single(s => s.GameDate.Date == gameDate.Date);
            else if (g.Any(a => a.GameDate > gameDate.AddMinutes(-40) && a.GameDate < gameDate.AddMinutes(30)))
                return g.First(a => a.GameDate > gameDate.AddMinutes(-30) && a.GameDate < gameDate.AddMinutes(30));
            return g.OrderByDescending(o => o.GameDate).FirstOrDefault();

        }
        public List<HittingStatsRow> GetSeasonHittingStats(int year, bool playoffs = false, DateTime? toDate = null)
        {
            HittingStatsRow model = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<HittingStats>().JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Profile, () => player)
                .Where(Restrictions.Eq(NhProjections.Year(() => game.GameDate), year))
                .Where(() => game.Playoff == playoffs).Where(() => !player.IsHidden);
            if (year == CurrentYear)
                query = query.Where(() => player.Current);
            if (toDate.HasValue)
                query = query.Where(() => game.GameDate < toDate);

            query = GetHittingSelectList(query, Projections.GroupProperty(Projections.Property(() => player.Id)), () => model.Id);

            var list = query.List<HittingStatsRow>().ToList();

            list.ForEach(a => a.DisplayAll(true));

            if (list.Any())
                AddHittingTotals(list);
            list.ForEach(f => f.Player = Player.Create(f.UniformNumber, f.FirstName, f.LastName));
            return list;
            //                string commandstring = @"select p.last_name,SUM(h.AB),SUM(h.R),SUM(h.H),SUM(h.2B),SUM(h.3B),Sum(h.HR),SUM(h.RBI),
            //SUM(h.BB),SUM(h.HBP),SUM(h.K),SUM(h.SB),SUM(h.CS),SUM(h.SAC),SUM(h.SF),SUM(LOB),p.first_name,p.player_id
            // from hittingstats h join gameschedule g on g.game_id=h.game_id join players p on p.player_id=h.player_id where year(game_date)=@param2 and playoff=@param1
            //group by p.last_name,p.first_name,year(game_date),g.playoff";
        }
        public async Task<IList<HittingStatsRow>> GetSeasonHittingStatsAsync(int year, bool playoffs = false, DateTime? toDate = null)
        {
            HittingStatsRow model = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<HittingStats>().JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Profile, () => player)
                .Where(Restrictions.Eq(NhProjections.Year(() => game.GameDate), year))
                .Where(() => game.Playoff == playoffs).Where(() => !player.IsHidden);
            if (year == CurrentYear)
                query = query.Where(() => player.Current);
            if (toDate.HasValue)
                query = query.Where(() => game.GameDate < toDate);

            query = GetHittingSelectList(query, Projections.GroupProperty(Projections.Property(() => player.Id)), () => model.Id);

            var list = await query.ListAsync<HittingStatsRow>();

            list.ToList().ForEach(f =>
            {
                f.Player = Player.Create(f.UniformNumber, f.FirstName, f.LastName);
                f.DisplayAll(true);
            });

            if (list.Any())
                AddHittingTotals(list);

            return list;
        }
        public IList<PitchingStatsRow> GetSeasonPitchingStats(int year, bool playoffs = false)
        {
            PitchingStatsRow row = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<PitchingStats>()
                .JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Player, () => player)
                .Where(Restrictions.Eq(NhProjections.Year(() => game.GameDate), year))
                .Where(() => game.Playoff == playoffs).Where(() => !player.IsHidden);

            query = GetPitchingSelectList(query, year, false, () => row.Id);
            var list = query.List<PitchingStatsRow>().ToList();
            list.ForEach(a => a.DisplayAll(true));
            if (list.Any())
                AddPitchingTotals(list);

            return list;
            //                string commandstring = @"select p.last_name,count(*) as G,
            //(select count(*) from pitchingstats ps join gameschedule g1 on g1.game_id=ps.game_id where year(g1.game_date)=@param2 and g1.playoff=@param1 and h.player_id=ps.player_id
            //and (decision='W' or decision='BS,W')) as W,
            //(select count(*) from pitchingstats ps join gameschedule g1 on g1.game_id=ps.game_id where year(g1.game_date)=@param2 and g1.playoff=@param1 and h.player_id=ps.player_id
            //and (decision='L' or decision='BS,L')) as L,
            //(select count(*) from pitchingstats ps join gameschedule g1 on g1.game_id=ps.game_id where year(g1.game_date)=@param2 and g1.playoff=@param1 and h.player_id=ps.player_id and decision='S') as S,
            //(select count(*) from pitchingstats ps join gameschedule g1 on g1.game_id=ps.game_id where year(g1.game_date)=@param2 and g1.playoff=@param1 and h.player_id=ps.player_id
            //and (decision='BS' or decision='S' or decision='BS,L' or decision='BS,W')) as SVO,

            //SUM(h.GS),SUM(h.IP),SUM(h.BF),SUM(h.H),Sum(h.R),SUM(h.ER),SUM(h.BB),SUM(h.K),SUM(h.HB),SUM(h.HR),SUM(h.CG),p.first_name,p.player_id
            // from pitchingstats h join gameschedule g on g.game_id=h.game_id join players p on p.player_id=h.player_id where year(game_date)=@param2 and playoff=@param1
            //group by p.last_name,p.first_name,year(game_date),g.playoff";

        }
        public async Task<IList<PitchingStatsRow>> GetSeasonPitchingStatsAsync(int year, bool playoffs = false)
        {
            PitchingStatsRow row = null;
            PlayerProfile player = null;
            GameData game = null;
            var query = Session.QueryOver<PitchingStats>()
                .JoinAlias(j => j.Game, () => game)
                .JoinAlias(j => j.Player, () => player)
                .Where(Restrictions.Eq(NhProjections.Year(() => game.GameDate), year))
                .Where(() => game.Playoff == playoffs).Where(() => !player.IsHidden);

            query = GetPitchingSelectList(query, year, false, () => row.Id);
            var list = await query.ListAsync<PitchingStatsRow>();
            list.ToList().ForEach(a => a.DisplayAll(true));
            if (list.Any())
                AddPitchingTotals(list);

            return list;
        }
        public bool UpdateGame(GameData game, IsolationLevel level)
        {
            return WrapInTryCatch(() =>
            {
                Session.SaveOrUpdate(game);
            }, level);
        }
        public List<HittingStatsRow> GetGameHittingStats(int gameId)
        {
            var stats = Session.Get<GameData>(gameId).HittingStats.Select(s => new HittingStatsRow(s)).ToList();
            if (stats.Any())
            {
                AddHittingTotals(stats);
                stats.SetDuplicatePlayers();
            }
            return stats;
            //               string commandstring = @"select p.last_name,h.AB,h.R,h.H,h.2B,h.3B,h.HR,h.RBI,h.BB,h.HBP,h.K,h.SB,h.CS,h.SAC,h.SF,LOB,p.first_name,p.player_id
            //from hittingstats h join gameschedule g on g.game_id=h.game_id join players p on p.player_id=h.player_id where  g.game_id=@param1 order by id ";
        }
        public List<PitchingStatsRow> GetGamePitchingStats(int gameId)
        {
            var stats = Session.Get<GameData>(gameId).PitchingStats.Select(s => new PitchingStatsRow(s)).ToList();

            if (stats.Any())
                AddPitchingTotals(stats);
            return stats;
            //               string commandstring = @"select p.last_name,'','','','','',h.GS,h.IP,h.BF,h.H,h.R,h.ER,h.BB,h.K,h.HB,h.HR,h.CG,p.first_name,p.player_id,h.Decision
            //from pitchingstats h join gameschedule g on g.game_id=h.game_id join players p on p.player_id=h.player_id
            //where  g.game_id=@param1 order by id";
        }
        public IEnumerable<int> Seasons()
        {
            PitchingStats stats = null;
            return Session.QueryOver<GameData>().Right.JoinAlias(j => j.PitchingStats, () => stats)
                .Select(NhProjections.Year<GameData>(g => g.GameDate))
                .OrderBy(NhProjections.Year<GameData>(o => o.GameDate)).Desc
                .List<int>().Distinct();
        }
        public IDictionary<DcPosition, List<DepthChart>> GetDepthChart(int? pos = null)
        {
            DepthChart model = null;
            PlayerProfile player = null;
            DepthChartRow dc = null;

            var query = Session.QueryOver(() => dc)
                .JoinAlias(j => j.Player, () => player)
                .SelectList(s => s.Select(() => dc.Position).WithAlias(() => model.Position)
                .Select(() => dc.Rank).WithAlias(() => model.Rank)
                .Select(() => player.Id).WithAlias(() => model.PlayerId)
                .Select(NhProjections.NameLastFirstInital(() => player).WithAlias(() => model.PlayerName)));
            if (pos.HasValue)
                query = query.Where(w => (int)w.Position == pos.Value);
            return query.TransformUsing(Transformers.AliasToBean<DepthChart>())
                .List<DepthChart>().GroupBy(g => g.Position)
                .ToDictionary(k => k.Key, v => v.OrderBy(o => o.Rank).ToList());
        }
        public IList<T> GetHistory<T>() where T : HistoryRow => Session.QueryOver<T>().OrderBy(o => o.Id).Asc.List();
        public IList<ManagerHistory> GetManagers()
        {
            var records = Session.CreateSQLQuery("SELECT * FROM managerrecords where Manager!='Total' order by Years")
                .List<object[]>().Select(ManagerHistory.Create).ToList();
            var playoffs = Session.CreateSQLQuery(
                @"SELECT concat_ws(' ',players.First_Name,players.Last_Name) as Manager,cast(Concat_ws('-',SUM(W) ,SUM(L)) as char(10)) as record
                                                            from season2 join gameschedule on gameschedule.Game_ID = season2.game_id
                                                            join history on history.YearStart = year(gameschedule.Game_Date) and history.Category = 'Manage'
                                                            join players on players.Player_ID = history.Data where gameschedule.Playoff = 1
                                                            group by history.Data").List<object[]>();
            foreach (var manager in records)
            {
                var playoffRecord = playoffs.SingleOrDefault(a => a[0] as string == manager.Data);
                if (playoffRecord != null)
                    manager.PlayoffRecord = playoffRecord[1] as string;
                else
                    manager.PlayoffRecord = "N/A";
            }

            return records;
        }
        public IList<ResultsHistory> GetResults() => Session.CreateSQLQuery("SELECT * FROM recordhistory order by Year").List<object[]>().Select(ResultsHistory.Create).ToList();
        public IList<PlayoffHistory> GetPlayoffs() => Session.CreateSQLQuery("SELECT * FROM playoffhistory order by Year").List<object[]>().Select(PlayoffHistory.Create).ToList();
        public IEnumerable<GameData> GetGamesByMonth(int month, int year) => Session.QueryOver<GameData>().Where(Restrictions.Eq(NhProjections.Year<GameData>(g => g.GameDate), year))
                .Where(Restrictions.Eq(NhProjections.Month<GameData>(m => m.GameDate), month)).List();//                string commandstring = @"select gs.game_id,gs.game_date,gs.opponent,gs.hv,gs.Location,rh.r as Hscore,ra.r as Ascore,l.shortName from gameschedule gs //left outer join rhe rh on rh.game_id=gs.game_id and rh.hv='H'//left outer join rhe ra on ra.game_id=gs.game_id and ra.hv='V'//join Locations l on l.id=gs.locationId where year(game_date)=@yr and month(game_date)=@mo";
        public IEnumerable<GameData> GetGamesByYear(int year) => Session.QueryOver<GameData>().Where(Restrictions.In(NhProjections.Year<GameData>(y => y.GameDate), new[] { year, year + 100 })).List();
        //public async Task<IEnumerable<GameData>> GetGamesByYearAsync(int year) => Task..Run(Session.QueryOver<GameData>().Where(Restrictions.In(NhProjections.Year<GameData>(y => y.GameDate), new[] { year, year + 100 })).List());
        public Dictionary<DateTime, string> GetEventsByMonth(int month, int year) => Session.QueryOver<ScheduleEvent>().Where(Restrictions.Eq(NhProjections.Year<ScheduleEvent>(s => s.Date), year))
                .Where(Restrictions.Eq(NhProjections.Month<ScheduleEvent>(s => s.Date), month))
                .List().ToDictionary(k => k.Date, v => v.Event);
        public Dictionary<string, string> GetFieldLinks() => Session.QueryOver<Location>()
                .SelectList(s => s.Select(NhProjections.ConcatWs<Location>(" - ", l => l.ShortFieldName, l => l.Field)).Select(l => l.Link))
                .Where(w => w.Current)
                .List<object[]>().ToDictionary(k => k[0].ToString(), v => v[1].ToString());
        private bool WrapInTryCatch(Action code, IsolationLevel level = IsolationLevel.ReadUncommitted)
        {
            try
            {
                Session.BeginTransaction(level);
                code.Invoke();
                Session.GetCurrentTransaction().Commit();
                return true;
            }
            catch (Exception)
            {
                Session.GetCurrentTransaction().Rollback();
                return false;
            }
        }
        public async Task<DateTime> GetStatsLastUpdatedAsync()
        {
            var p = await Session.GetAsync<PlayerProfile>(0);
            return p.DOB.Value;
        }
        private async Task<T> GetCareerStatsFromCacheAsync<T>(string cacheKey, Func<Task<T>> queryMethod) where T : IEnumerable<IDisplayToggleable>
        {
            if (!_cache.TryGetValue(cacheKey, out T stats))
            {
                stats = await queryMethod();
                if (stats != null)
                {
                    foreach (var item in stats)
                        item.DisplayAll(true);
                }
                var cacheEntryOptions = new MemoryCacheEntryOptions().AddExpirationToken(new CancellationChangeToken(_cancellationTokenSource.Token));
                _cache.Set(cacheKey, stats, cacheEntryOptions);
            }
            return stats;
        }
        public async Task<IList<HittingStatsRow>> GetCareerHittingStatsFromCacheAsync()
        {
            return await GetCareerStatsFromCacheAsync("HittingRecords", GetCareerHittingStatsAsync);
        }
        public async Task<IList<PitchingStatsRow>> GetCareerPitchingStatsFromCacheAsync()
        {
            return await GetCareerStatsFromCacheAsync("PitchingRecords", GetCareerPitchingStatsAsync);
        }
        public void ResetRecordsCache()
        {
            lock (_lock)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }
        public async Task<IList<YearlySummary>> GetHistoricalTrendsAsync(int startYear)
        {
            YearlySummary model = null;
            GameData game = null;
            HittingStats h = null;
            var summaries = await _session.QueryOver(() => h).JoinAlias(() => h.Game, () => game)
                .Where(Restrictions.Gt(NhProjections.Year(() => game.GameDate), startYear))
                .Where(() => game.Playoff == false)
                .SelectList(s => s.Select(Projections.GroupProperty(NhProjections.Year(() => game.GameDate)).WithAlias(() => model.Year))
                    .SelectSum(() => h.Hits).WithAlias(() => model.TotalHits)
                    .SelectSum(() => h.Runs).WithAlias(() => model.TotalRuns)
                    .SelectSum(() => h.HomeRuns).WithAlias(() => model.TotalHomeRuns)
                    .SelectSum(() => h.StrikeOuts).WithAlias(() => model.TotalStrikeOuts)
                    .SelectSum(() => h.Doubles).WithAlias(() => model.TotalDoubles)
                    .SelectSum(() => h.Triples).WithAlias(() => model.TotalTriples)
                    ).TransformUsing(Transformers.AliasToBean<YearlySummary>())
                .ListAsync<YearlySummary>();
            return summaries;
        }
        public async Task<IList<YearlySummary>> GetPitchingTrendsAsync(int startYear)
        {
            YearlySummary model = null;
            GameData game = null;
            PitchingStats h = null;
            var summaries = await _session.QueryOver(() => h).JoinAlias(() => h.Game, () => game)

                .Where(Restrictions.Gt(NhProjections.Year(() => game.GameDate), startYear))
                .Where(() => game.Playoff == false)
                .SelectList(s => s.Select(Projections.GroupProperty(NhProjections.Year(() => game.GameDate)).WithAlias(() => model.Year))
                    .SelectSum(() => h.Hits).WithAlias(() => model.TotalHits)
                    .SelectSum(() => h.Runs).WithAlias(() => model.TotalRuns)
                    .SelectSum(() => h.StrikeOuts).WithAlias(() => model.TotalStrikeOuts)
                    ).TransformUsing(Transformers.AliasToBean<YearlySummary>())
                .ListAsync<YearlySummary>();
            return summaries;
        }
        public bool CreateDepthChart(DepthChartDto dto)
        {
            return WrapInTryCatch(() =>
            {
                var dc = DepthChartRow.Create(dto.Position, dto.Rank, Session.Load<PlayerProfile>(dto.PlayerId));
                Session.Save(dc);
            });
        }
        public bool UpdateDepthChart(List<DepthChartDto> dtos)
        {
            return WrapInTryCatch(() =>
            {
                var dtoDict = dtos.ToDictionary(k => k.Rank, v => v);
                var existing = Session.QueryOver<DepthChartRow>().Where(w => (int)w.Position == dtos.First().Position).List();
                foreach (var dto in existing)
                {
                    if (dtoDict.TryGetValue(dto.Rank, out var newDto))
                    {
                        dto.Update(Session.Load<PlayerProfile>(newDto.PlayerId));
                        Session.SaveOrUpdate(dto);
                    }
                }
            });
        }
        public bool DeleteDepthChart(int id)
        {
            return WrapInTryCatch(() =>
            {
                var entity = Session.Get<DepthChartRow>(id);
                if (entity != null)
                    Session.Delete(entity);
            });
        }
    }
}

