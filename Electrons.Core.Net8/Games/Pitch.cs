using System;

namespace Electrons.Core.Net8.Games
{
    public class Pitch : InningEvent
    {
        protected Pitch() { }
        public PitchResult Result { get; protected set; }
        public static Pitch CalledStrike => new CalledStrike();
        public static Pitch Ball => new PitchedBall();
        public static Pitch Foul => new FoulBall();
        public static Pitch SwingingStrike => new SwingingStrike();
        public static Pitch InPlay => new PitchInPlay();
        public static Pitch GetPitch(PitchResult pitch)
        {
            switch (pitch)
            {
                case PitchResult.Ball:
                    return Ball;
                case PitchResult.CalledStrike:
                    return CalledStrike;
                case PitchResult.SwingingStrike:
                    return SwingingStrike;
                case PitchResult.Foul:
                    return Foul;
                case PitchResult.InPlay:
                    return InPlay;
                default:
                    throw new ArgumentException();
            }
        }
        public override string EventString(Player batter) => EventText;
        internal override string EventText => Result.GetDescription();

        public override string ToString() => Result.GetDescription();
    }
    internal class PitchInPlay : Pitch
    {
        public PitchInPlay() => Result = PitchResult.InPlay;
    }
    internal class SwingingStrike : Pitch
    {
        public SwingingStrike() => Result = PitchResult.SwingingStrike;
    }
    internal class FoulBall : Pitch
    {
        public FoulBall() => Result = PitchResult.Foul;
    }
    internal class PitchedBall : Pitch
    {
        public PitchedBall() => Result = PitchResult.Ball;
    }
    internal class CalledStrike : Pitch
    {
        public CalledStrike() => Result = PitchResult.CalledStrike;
    }
}
