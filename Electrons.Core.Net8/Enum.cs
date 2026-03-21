using System;
using System.ComponentModel;

namespace Electrons.Core.Net8
{
    public enum HV { H, V };
    public enum Bats { R = 0, L = 1, S = 2 };
    public enum Throws { R = 0, L = 1 };
    public enum Decision
    {
        [Description("")]
        ND = 0,
        W = 1,
        L = 2,
        S = 3,
        BS = 4,
        H = 5,
        [Description("BS,L")]
        BSL = 6,
        [Description("BS,W")]
        BSW = 7
    }
    public enum DuesType
    {
        Dues = 1,
        Uniform = 2,
        Additional = 3,
        PitcherOnly = 4
    }
    public enum HittingCategories
    {
        [StatProperty("AtBats", "At Bats")]
        AB = 1,
        [StatProperty("Runs")]
        R = 2,
        [StatProperty("Hits")]
        H = 3,
        [Description("2B"), StatProperty("Doubles")]
        _2B = 4,
        [Description("3B"), StatProperty("Triples")]
        _3B = 5,
        [StatProperty("HomeRuns", "Home Runs")]
        HR = 6,
        [StatProperty("Rbis", "Runs Batted In")]
        RBI = 7,
        [StatProperty("Walks")]
        BB = 8,
        [StatProperty("Hbp", "HBP")]
        HBP = 9,
        [StatProperty("StrikeOuts")]
        K = 10,
        [StatProperty("StolenBases", "Stolen Bases")]
        SB = 11,
        [StatProperty("CaughtStealing", "Caught Stealing")]
        CS = 12,
        [StatProperty("SacBunts", "Sac Bunts")]
        SAC = 13,
        [StatProperty("SacFlies", "Sac Flies")]
        SF = 14,
        [StatProperty("LeftOnBase", "Left On Base")]
        LOB = 15,
        [StatProperty("BattingAverage", "Batting Average")]
        BA = 16,
        [StatProperty("Slugging", "Slugging Pct.")]
        SLG = 17,
        [StatProperty("OnBasePct", "On Base Pct.")]
        OBP = 18,
        [StatProperty("Ops", "OPS")]
        OPS = 19

    }
    public class StatPropertyAttribute : Attribute
    {
        public StatPropertyAttribute(string name, string display = null, bool qualifier = false)
        {
            Name = name;
            Display = display ?? name;
            Qualifier = qualifier;
        }
        public string Name;
        public string Display;
        public bool SortAscending;
        public bool Qualifier;
    }
    public enum PitchingCategories
    {
        [StatProperty("Wins", "Wins")]
        Wins = 1,
        [StatProperty("Losses")]
        Losses = 2,
        [StatProperty("Saves", "Saves")]
        Saves = 3,
        [StatProperty("SaveOpportunities", "Save Opps.")]
        SvO = 4,
        [StatProperty("Games", "Appearance")]
        G = 5,
        [StatProperty("DisplayInnings", "Innings")]
        IP = 6,
        [StatProperty("Hits", SortAscending = true, Qualifier = true)]
        H = 7,
        [StatProperty("Runs", SortAscending = true, Qualifier = true)]
        R = 8,
        [StatProperty("EarnedRuns", "Earned Runs", SortAscending = true, Qualifier = true)]
        ER = 9,
        [StatProperty("Walks", SortAscending = true, Qualifier = true)]
        BB = 10,
        [StatProperty("StrikeOuts", "Strike Outs")]
        K = 11,
        [StatProperty("HitBatters", "Hit Batters")]
        HB = 12,
        [StatProperty("HomeRuns", "Home Runs")]
        HR = 13,
        [StatProperty("BattersFaced", "Batters Faced")]
        BF = 14,
        [StatProperty("CompleteGames", "Complete Games")]
        CG = 15,
        [StatProperty("EarnedRunAvg", "Earned Run Avg.", SortAscending = true, Qualifier = true)]
        ERA = 16,
        [StatProperty("Whip", "WHIP", SortAscending = true, Qualifier = true)]
        WHIP = 17,
        [StatProperty("StrikeOutsPer9", "K/9", Qualifier = true), Description("K /9")]
        K9 = 18,
        [StatProperty("WalksPer9", "BB/9", SortAscending = true, Qualifier = true), Description("BB /9")]
        BB9 = 19
    }
    public enum StatsCategory
    {
        Season = 1,
        Career = 2,
        Playoff = 3
    }
    public enum HalfInning
    {
        Top = 0,
        Bottom = 1
    }

    public enum PitchResult
    {
        [Description("Strike swinging.  ")]
        SwingingStrike = 0,
        [Description("Called strike.  ")]
        CalledStrike = 1,
        [Description("Ball.  ")]
        Ball = 2,
        [Description("Foul.  ")]
        Foul = 3,
        [Description("In play.  ")]
        InPlay = 4
    }
    [Flags]
    public enum OnBase
    {
        [Description("home")]
        None = 0,
        First = 1,
        Second = 2,
        Third = 4,
        Loaded = 7
    }
    public enum FieldLocation
    {
        Undefined = 0,
        [Description("infield")]
        Infield,
        [Description("left")]
        Left,
        [Description("deep left")]
        DeepLeft,
        [Description("center")]
        Center,
        [Description("deep center")]
        DeepCenter,
        [Description("right")]
        Right,
        [Description("deep right")]
        DeepRight
    }

    public enum DcPosition
    {
        SP = 1,
        RP = 10,
        C = 2,
        _1B = 3,
        _2B = 4,
        _3B = 5,
        SS = 6,
        LF = 7,
        CF = 8,
        RF = 9
    }
}
