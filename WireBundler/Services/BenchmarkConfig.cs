namespace WireBundler.Services
{
    public class BenchmarkConfig
    {
        public double MinFineAngularOffset { get; set; } = 0.0;
        public double MaxFineAngularOffset { get; set; } = 15.0;
        public double FineAngularOffsetStep { get; set; } = 1.0;
        public int SurvivorCountLimit { get; set; } = 2;
        public int FallbackDirectionStart { get; set; } = 4;
        public int FallbackDirectionLimit { get; set; } = 12;
        public int FallbackDirectionStep { get; set; } = 1;
    }
}