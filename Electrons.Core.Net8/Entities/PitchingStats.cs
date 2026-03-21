using NHibernate.Mapping.Attributes;
using System;

namespace Electrons.Core.Net8.Entities
{
    [Class(Table = "pitchingstats")]
    public class PitchingStats
    {
        public PitchingStats() { }
        protected PitchingStats(GameData game, PlayerProfile player)
        {
            Game = game;
            Player = player;
        }

        [Id(Column = "Id", Name = "Id"), Generator(Class = "native")]
        public virtual int Id { get; protected set; }
        [ManyToOne(Column = "Player_ID", ClassType = typeof(PlayerProfile), NotFound = NotFoundMode.Ignore)]
        public virtual PlayerProfile Player { get; protected set; }
        [ManyToOne(Column = "Game_ID", ClassType = typeof(GameData))]
        public virtual GameData Game { get; protected set; }
        [Property(Column = "Decision")]
        public virtual string DecisionVal { get; protected set; }
        public virtual Decision Decision
        {
            get
            {
                if (string.IsNullOrEmpty(DecisionVal))
                    return Decision.ND;
                return (Decision)Enum.Parse(typeof(Decision), DecisionVal);
            }
        }
        [Property(Column = "GS")]
        public virtual int GameStarted { get; protected set; }
        [Property(Column = "IP")]
        public virtual decimal InningsPitched { get; protected set; }
        [Property(Column = "BF")]
        public virtual int BattersFaced { get; protected set; }
        [Property(Column = "H")]
        public virtual int Hits { get; protected set; }
        [Property(Column = "R")]
        public virtual int Runs { get; protected set; }
        [Property(Column = "ER")]
        public virtual int EarnedRuns { get; protected set; }
        [Property(Column = "BB")]
        public virtual int Walks { get; protected set; }
        [Property(Column = "K")]
        public virtual int StrikeOuts { get; protected set; }
        [Property(Column = "HB")]
        public virtual int HitBatters { get; protected set; }
        [Property(Column = "HR")]
        public virtual int HomeRuns { get; protected set; }
        [Property(Column = "CG")]
        public virtual int CompleteGames { get; protected set; }
        public override string ToString()
        {
            return $"{Player.LastName}, {Game.GameDate.ToShortDateString()} vs. {Game.Opponent}";
        }
        internal static PitchingStats CreateNew(GameData game, PlayerProfile player, string dec)
        {
            return new PitchingStats(game, player)
            {
                DecisionVal = dec
            };
        }

        protected internal virtual void Update(PitchingStatsRow stats, string dec)
        {
            GameStarted = stats.Starts.GetValueOrDefault();
            InningsPitched = stats.Innings;
            BattersFaced = stats.BattersFaced.GetValueOrDefault();
            Hits = stats.Hits.GetValueOrDefault();
            Runs = stats.Runs.GetValueOrDefault();
            EarnedRuns = stats.EarnedRuns.GetValueOrDefault();
            Walks = stats.Walks.GetValueOrDefault();
            StrikeOuts = stats.StrikeOuts.GetValueOrDefault();
            HitBatters = stats.HitBatters.GetValueOrDefault();
            HomeRuns = stats.HomeRuns.GetValueOrDefault();
            CompleteGames = stats.CompleteGames.GetValueOrDefault();
            DecisionVal = dec;
        }
    }
}
