namespace WireBundler.Services
{
    public class BenchmarkConfig
    {
        public int FallbackDirectionStart { get; init; }
        public int FallbackDirectionLimit { get; init; }
        public int FallbackDirectionStep { get; init; }
        public int SurvivorCountLimit { get; init; }
        public double MinFineAngularOffset { get; init; }
        public double MaxFineAngularOffset { get; init; }
        public double FineAngularOffsetStep { get; init; }

        public string FallbackDescription =>
            $"Fallback directions: {FallbackDirectionStart} .. {FallbackDirectionLimit} step {FallbackDirectionStep}";

        public string SurvivorDescription =>
            $"Coarse survivors: 1 .. {SurvivorCountLimit}";

        public string FineOffsetDescription =>
            $"Fine offset: {MinFineAngularOffset} .. {MaxFineAngularOffset} step {FineAngularOffsetStep}";
    }
}