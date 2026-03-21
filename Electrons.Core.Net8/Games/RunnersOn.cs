using System.Collections.Generic;
using System.Linq;

namespace Electrons.Core.Net8.Games
{
    public class RunnersOn
    {
        private readonly Dictionary<OnBase, BaseRunner> _runnerDict;

        public RunnersOn()
        {
            _runnerDict = new Dictionary<OnBase, BaseRunner>
            {
                { OnBase.First, null },
                { OnBase.Second, null },
                { OnBase.Third, null }
            };
        }
        public BaseRunner OnFirst => _runnerDict[OnBase.First];
        public BaseRunner OnSecond => _runnerDict[OnBase.Second];
        public BaseRunner OnThird => _runnerDict[OnBase.Third];
        public BaseRunner this[OnBase onbase] => _runnerDict[onbase];
        public OnBase Runners
        {
            get
            {
                OnBase ob = OnBase.None;
                if (!(OnFirst is null))
                    ob |= OnBase.First;
                if (!(OnSecond is null))
                    ob |= OnBase.Second;
                if (!(OnThird is null))
                    ob |= OnBase.Third;
                return ob;
            }
        }
        public IEnumerable<BaseRunner> RunnersOnBase
        {
            get
            {
                if (RunnerOnFirst)
                    yield return OnFirst;
                if (RunnerOnSecond)
                    yield return OnSecond;
                if (RunnerOnThird)
                    yield return OnThird;
            }
        }
        public int Count => (OnFirst is null ? 0 : 1) + (OnSecond is null ? 0 : 1) + (OnThird is null ? 0 : 1);
        internal void PinchRun(Player onBase, Player runner)
        {
            if (OnFirst?.Runner == onBase)
                _runnerDict[OnBase.First].SetRunner(runner);
            if (OnSecond?.Runner == onBase)
                _runnerDict[OnBase.Second].SetRunner(runner);
            if (OnThird?.Runner == onBase)
                _runnerDict[OnBase.Third].SetRunner(runner);
        }
        public bool RunnerOnThird => OnThird is BaseRunner;
        public bool RunnerOnSecond => OnSecond is BaseRunner;
        public bool RunnerOnFirst => OnFirst is BaseRunner;
        internal void AdvanceRunners(RunningEvent ev, bool error)
        {
            var advancingPlayer = ev.Player;
            var initialBase = _runnerDict.SingleOrDefault(s => s.Value?.Runner == advancingPlayer);
            if (initialBase.Key != OnBase.None)
                _runnerDict[initialBase.Key] = null;
            if (ev.AdvanceTo != OnBase.None)
                _runnerDict[ev.AdvanceTo] = BaseRunner.Create(advancingPlayer, ev.ResponsiblePitcher, ev.OriginalRunner, error);
        }
        public override string ToString() => Runners.ToString();
    }
    public class BaseRunner
    {
        public Player Runner { get; private set; }
        public Player ResponsiblePitcher { get; private set; }
        public Player OriginalRunner { get; private set; }
        internal void SetRunner(Player runner)
        {
            Runner = runner;
        }
        public bool ReachedOnError { get; private set; }
        internal static BaseRunner Create(Player advancingPlayer, Player responsiblePitcher, Player originalRunner = null, bool error = false)
        {
            return new BaseRunner
            {
                Runner = advancingPlayer,
                ResponsiblePitcher = responsiblePitcher,
                ReachedOnError = error,
                OriginalRunner = originalRunner ?? advancingPlayer
            };
        }
        public override bool Equals(object obj)
        {
            if (!(obj is BaseRunner br))
                return false;
            return Runner.Equals(br.Runner) && ResponsiblePitcher == br.ResponsiblePitcher;
        }
        public static bool operator ==(BaseRunner lhs, BaseRunner rhs)
        {
            if (ReferenceEquals(lhs, rhs))
                return true;
            if (lhs is null)
                return false;
            return lhs.Equals(rhs);
        }
        public static bool operator !=(BaseRunner lhs, BaseRunner rhs)
        {
            return !(lhs == rhs);
        }
        public override int GetHashCode()
        {
            return Runner.GetHashCode() * ResponsiblePitcher.GetHashCode();
        }
        public override string ToString() => Runner.ToString();

    }
}
