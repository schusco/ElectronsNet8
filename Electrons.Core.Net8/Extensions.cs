using Electrons.Core.Net8.Entities;
using Electrons.Core.Net8.Games;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Electrons.Core.Net8
{
    public static class Extensions
    {
        public static string Capitalize(this string input)
        {
            return string.Join(" ", input.Split(' ').Select(s => s.ToCamelCaseString()));
        }
        private static string ToCamelCaseString(this string input)
        {
            return $"{input[0].ToString().ToUpper()}{input.Substring(1, input.Length - 1).ToLower()}";
        }
        public static string ReplaceLast(this string input, string oldValue, string newValue)
        {
            var place = input.LastIndexOf(oldValue, StringComparison.Ordinal);
            return place == -1 ? input : input.Remove(place, oldValue.Length).Insert(place, newValue);
        }
        public static string Combine(this string input, char separator, params string[] data)
        {
            StringBuilder sb = new StringBuilder(input);
            foreach (var str in data)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    sb.Append(separator);
                    sb.Append(str);
                }
            }
            return sb.ToString().TrimStart(',');
        }
        public static string TimeLength(this TimeSpan input)
        {
            var hours = Math.Floor(input.TotalHours);
            var mins = Math.Floor(input.TotalMinutes) % 60;
            return $"{hours} {(hours == 1 ? "hour" : "hours")}, {mins} {(mins == 1 ? "minute" : "minutes")}";
        }
        internal static string GetMemberName(this Expression expression)
        {
            string currentVal = "";
            while (true)
            {
                var memberExpression = expression as MemberExpression ?? (MemberExpression)((UnaryExpression)expression).Operand;
                currentVal = currentVal == "" ? memberExpression.Member.Name : $"{memberExpression.Member.Name}.{currentVal}";
                if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
                {
                    expression = memberExpression.Expression;
                    continue;
                }
                break;
            }
            return currentVal;
        }
        public static TimeSpan SumTimeSpan<T>(this IEnumerable<T> input, Expression<Func<T, TimeSpan>> expression)
        {
            var ts = new TimeSpan();
            var prop = typeof(T).GetProperty(expression.Body.GetMemberName());
            foreach (var obj in input)
                ts = ts.Add((TimeSpan)prop.GetValue(obj));
            return ts;
        }
        public static IEnumerable<HittingStatsRow> SumHitting(this IEnumerable<HittingStats> input, Func<HittingStats, object> expression)
        {
            var list = input.GroupBy(expression).Select(HittingStatsRow.Sum).ToList();
            list.ForEach(firstRow =>
            {
                firstRow.SumAb = input.Sum(s => s.AtBats);
                firstRow.SumR = input.Sum(s => s.Runs);
                firstRow.SumH = input.Sum(s => s.Hits);
                firstRow.SumHr = input.Sum(s => s.HomeRuns);
                firstRow.SumRbi = input.Sum(s => s.RunsBattedIn);
                firstRow.SumBb = input.Sum(s => s.Walks);
                firstRow.SumK = input.Sum(s => s.StrikeOuts);
                firstRow.SumSb = input.Sum(s => s.StolenBases);
                firstRow.TotalBa = Utilities.CalculateBa(firstRow.SumH, firstRow.SumAb);
                firstRow.TotalSlg = Utilities.CalculateSlg(input.Sum(s => (decimal)Utilities.CalculateTB(s.Hits, s.Doubles, s.Triples, s.HomeRuns)), input.Sum(s => s.AtBats));
                firstRow.TotalObp = Utilities.CalculateObp(input.Sum(s => s.Hits), input.Sum(s => s.Walks), input.Sum(s => s.HitByPitches), input.Sum(s => s.AtBats), input.Sum(s => s.SacFlies));
                firstRow.TotalOps = firstRow.TotalSlg + firstRow.TotalObp;
                firstRow.SumLob = input.Sum(s => s.LeftOnBase);
                firstRow.SumSf = input.Sum(s => s.SacFlies);
                firstRow.SumSac = input.Sum(s => s.SacrificeBunts);
                firstRow.SumCs = input.Sum(s => s.CaughtStealing);
                firstRow.SumHbp = input.Sum(s => s.HitByPitches);
                firstRow.Sum3b = input.Sum(s => s.Triples);
                firstRow.Sum2b = input.Sum(s => s.Doubles);
                firstRow.SumG = input.Count();
            });
            return list;
        }
        public static string NumberString(this int input)
        {
            if (input < 1)
                throw new BaseballGameException("Invalid inning Number");
            if (input == 1)
                return "1st";
            if (input == 2)
                return "2nd";
            if (input == 3)
                return "3rd";
            return $"{input}th";
        }
        public static bool IsIn<T>(this T input, params T[] vals) => vals.Contains(input);
        public static bool IsIn<T>(this T input, IEnumerable<T> vals) => vals.Contains(input);
        public static IEnumerable<PitchingStatsRow> SumPitching(this IEnumerable<PitchingStats> input, Func<PitchingStats, object> expression)
        {
            var list = input.GroupBy(expression).Select(PitchingStatsRow.Sum).ToList();
            list.ForEach(firstRow =>
            {
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
            });
            return list;
        }
        public static string ToHeightString(this int input)
        {
            return (input / 12).ToString() + "' " + (input % 12).ToString() + '"';
        }
        public static bool IncludeColumn<T>(this PropertyInfo prop, IEnumerable<T> data)
        {
            if (!prop.GetOptional()) return true;
            return data.Any(a => prop.GetValue(a, null) != null);
        }
        public static bool GetOptional(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(TableColumnAttribute), false).SingleOrDefault();
            return attribute != null && ((TableColumnAttribute)attribute).Optional;
        }
        public static string GetDescription(this Enum input)
        {
            var type = input.GetType();
            var memInfo = type.GetMember(input.ToString());
            var attr = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attr == null || attr.Length == 0)
                return memInfo[0].Name;
            return ((DescriptionAttribute)attr[0]).Description;
        }
        public static string GetPropertyName(this Enum input)
        {
            var type = input.GetType();
            var memInfo = type.GetMember(input.ToString());
            var attr = memInfo[0].GetCustomAttributes(typeof(StatPropertyAttribute), false);
            if (attr == null || attr.Length == 0)
                return memInfo.ToString();
            return ((StatPropertyAttribute)attr[0]).Name;
        }
        public static string GetPropertyDisplayName(this Enum input)
        {
            var type = input.GetType();
            var memInfo = type.GetMember(input.ToString());
            var attr = memInfo[0].GetCustomAttributes(typeof(StatPropertyAttribute), false);
            if (attr == null || attr.Length == 0)
                return memInfo.ToString();
            var statAttr = (StatPropertyAttribute)attr[0];
            return statAttr.Display ?? statAttr.Name;
        }
        public static object GetPropertyValue(this HittingStatsRow row, HittingCategories stat)
        {
            var prop = stat.GetPropertyName();
            var props = row.GetType().GetProperties();
            var instProp = props.Single(w => w.Name == prop);
            return instProp.GetValue(row);
        }
        public static object GetPropertyValue(this PitchingStatsRow row, PitchingCategories stat)
        {
            var prop = stat.GetPropertyName();
            var props = row.GetType().GetProperties();
            var instProp = props.Single(w => w.Name == prop);
            return instProp.GetValue(row);
        }
        public static bool GetStatSort(this Enum input)
        {
            var statAttr = input.GetStatAttribute();
            return statAttr?.SortAscending ?? false;
        }
        private static StatPropertyAttribute GetStatAttribute(this Enum input)
        {
            var type = input.GetType();
            var memInfo = type.GetMember(input.ToString());
            var attr = memInfo.FirstOrDefault()?.GetCustomAttributes(typeof(StatPropertyAttribute), false);
            if (attr == null || attr.Length == 0)
                return null;
            return (StatPropertyAttribute)attr[0];
        }
        public static bool HasQualifier(this Enum input)
        {
            var statAttr = input.GetStatAttribute();
            return statAttr?.Qualifier ?? false;
        }
        public static string GetFormat<T>(this Enum input)
        {
            var attr= input.GetStatAttribute();
            if (attr == null)
                return "";
            var statPropertyName = attr.Name;
            var pitchingType = typeof(T);
            var prop = pitchingType.GetProperties().Single(s => s.Name == statPropertyName);
            var statAttr = prop.GetCustomAttributes().Single(w => w is TableColumnAttribute) as TableColumnAttribute;
            return statAttr.Format;
        }
        public static IEnumerable<HittingStatsRow> Combine(this IEnumerable<HittingStatsRow> rows)
        {
            var groups = rows.GroupBy(g => g.Id);
            foreach (var row in groups)
            {
                var newrow = new HittingStatsRow
                {
                    FirstName = row.First().FirstName,
                    LastName = row.First().LastName,
                    AtBats = row.Sum(s => s.AtBats),
                    Runs = row.Sum(s => s.Runs),
                    Hits = row.Sum(s => s.Hits),
                    Doubles = row.Sum(s => s.Doubles),
                    Triples = row.Sum(s => s.Triples),
                    HomeRuns = row.Sum(s => s.HomeRuns),
                    Rbis = row.Sum(s => s.Rbis),
                    Walks = row.Sum(s => s.Walks),
                    StrikeOuts = row.Sum(s => s.StrikeOuts),
                    Hbp = row.Sum(s => s.Hbp),
                    StolenBases = row.Sum(s => s.StolenBases),
                    CaughtStealing = row.Sum(s => s.CaughtStealing),
                    SacBunts = row.Sum(s => s.SacBunts),
                    SacFlies = row.Sum(s => s.SacFlies),
                    LeftOnBase = row.Sum(s => s.LeftOnBase)
                };
                newrow.DisplayAll(true);
                yield return newrow;
            }
        }
        public static IEnumerable<PitchingStatsRow> Combine(this IEnumerable<PitchingStatsRow> rows)
        {
            var groups = rows.GroupBy(g => g.Id);
            foreach (var row in groups)
            {
                var newrow = new PitchingStatsRow
                {
                    FirstName = row.First().FirstName,
                    LastName = row.First().LastName,
                    Starts = row.Sum(s => s.Starts),
                    Games = row.Sum(s => s.Games),
                    CompleteGames = row.Sum(s => s.CompleteGames),
                    Innings = row.Sum(s => s.Innings),
                    Hits = row.Sum(s => s.Hits),
                    EarnedRuns = row.Sum(s => s.EarnedRuns),
                    Walks = row.Sum(s => s.Walks),
                    Runs = row.Sum(s => s.Runs),
                    StrikeOuts = row.Sum(s => s.StrikeOuts),
                    HitBatters = row.Sum(s => s.HitBatters),
                    HomeRuns = row.Sum(s => s.HomeRuns),
                    BattersFaced = row.Sum(s => s.BattersFaced),
                    Wins = row.Sum(s => s.Wins),
                    Losses = row.Sum(s => s.Losses),
                    Saves = row.Sum(s => s.Saves),
                    SaveOpportunities = row.Sum(s => s.SaveOpportunities)
                };
                newrow.DisplayAll(true);
                yield return newrow;
            }
        }
        public static IEnumerable<HStats> StatSum(this IEnumerable<HStats> input, Func<HStats, int> expression = null)
        {
            if (expression != null)
                return input.GroupBy(expression).Select(HStats.Sum);
            else
                return new List<HStats> { HStats.Sum(input) };
        }
        public static string GetLogo(this string input)
        {
            return string.Format(@"~/Content/images/logos/nextOuting_{0}.png", input.Replace(" ", "").ToLower());
        }
        public static void SetDuplicatePlayers(this IEnumerable<IHasPlayer> input)
        {
            var dupGroups = input.GroupBy(g => g.Player?.LastName);
            foreach (var dups in dupGroups)
            {
                foreach (var dup in dups.Skip(1))
                {
                    dup.Player.SetDuplicate();
                }
            }
        }
        public static DateTime Actual(this DateTime input)
        {
            TimeZoneInfo cst = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cst);
        }
    }

    public class EnumHelper
    {
        public static List<KeyValuePair<string, int>> GetList<T>()
        {
            Type enumType = typeof(T);

            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("T is not System.Enum");

            List<KeyValuePair<string, int>> enumValList = new List<KeyValuePair<string, int>>();

            foreach (var e in Enum.GetValues(typeof(T)))
            {
                var fi = e.GetType().GetField(e.ToString());
                var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

                enumValList.Add(new KeyValuePair<string, int>(attributes.Length > 0 ? attributes[0].Description : e.ToString(), (int)e));
            }

            return enumValList;
        }

    }
}
