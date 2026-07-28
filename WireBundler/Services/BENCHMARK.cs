using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using WireBundler.Models;
using WireBundler.Services;

namespace WireBundler.Services
{
    /// <summary>
    /// One-off benchmark harness for tuning WirePackingSolver parameters.
    /// Not intended for production use.
    /// </summary>
    public static class BENCHMARK
    {
        const double MinFineAngularOffset = 0.0; //0,180,1,20,4,180,1
        const double MaxFineAngularOffset = 15.0;
        const double FineAngularOffsetStep = 1.0;
        const int SurvivorCountLimit = 2;
        const int FallbackDirectionStart = 4;
        const int FallbackDirectionLimit = 12;
        const int FallbackDirectionStep = 1;

        public static BenchmarkConfig CurrentConfig =>
        new BenchmarkConfig
        {
            FallbackDirectionStart = FallbackDirectionStart,
            FallbackDirectionLimit = FallbackDirectionLimit,
            FallbackDirectionStep = FallbackDirectionStep,
            SurvivorCountLimit = SurvivorCountLimit,
            MinFineAngularOffset = MinFineAngularOffset,
            MaxFineAngularOffset = MaxFineAngularOffset,
            FineAngularOffsetStep = FineAngularOffsetStep
        };

        public static void RunSolverBenchmark(
            string inputFilePath,
            string orderLabel,
            Action<int, int, double?>? reportProgress,
            Action<(int fallbackDirections, int survivors, double fineOffset), double, long>? reportResult)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path must not be empty.");

            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException($"Input file not found: {inputFilePath}");

            InputParser parser = new InputParser();
            WirePackingSolver solver = new WirePackingSolver();

            InputData inputData = parser.LoadFromFile(inputFilePath);

            int fallbackCount = ((FallbackDirectionLimit - FallbackDirectionStart) / FallbackDirectionStep) + 1;
            int fineOffsetCount = (int)Math.Floor((MaxFineAngularOffset - MinFineAngularOffset) / FineAngularOffsetStep) + 1;
            int k = Math.Clamp(((SurvivorCountLimit - 1 - FallbackDirectionStart) / FallbackDirectionStep) + 1, 0, fallbackCount);
            int arithmeticSum = k * (2 * FallbackDirectionStart + (k - 1) * FallbackDirectionStep) / 2;
            int survivorSum = arithmeticSum + (fallbackCount - k) * SurvivorCountLimit;
            int totalCount = survivorSum * fineOffsetCount;

            int doneCount = 0;
            int sampleCount = Math.Min(100, totalCount);
            long sampleElapsedMs = 0;
            double estimatedTotalSeconds = 0.0;

            for (int fallbackDirections = FallbackDirectionStart; fallbackDirections <= FallbackDirectionLimit; fallbackDirections += FallbackDirectionStep)
            {
                int maxSurvivorsForThisFallback = Math.Min(SurvivorCountLimit, fallbackDirections);

                for (int survivors = 1; survivors <= maxSurvivorsForThisFallback; survivors++)
                {
                    for (double fineOffset = MinFineAngularOffset; fineOffset <= MaxFineAngularOffset; fineOffset += FineAngularOffsetStep)
                    {
                        solver.FallbackDirectionCount = fallbackDirections;
                        solver.CoarseSurvivorCount = survivors;
                        solver.FineAngularOffsetDegrees = fineOffset;

                        Stopwatch stopwatch = Stopwatch.StartNew();
                        BundleResult result = solver.Solve(inputData, orderLabel);
                        stopwatch.Stop();

                        double diameter = result.BundleDiameter;
                        long elapsedMs = stopwatch.ElapsedMilliseconds;

                        doneCount++;

                        if (doneCount <= sampleCount)
                        {
                            sampleElapsedMs += elapsedMs;

                            if (doneCount == sampleCount)
                            {
                                double avgPerRunMs = (double)sampleElapsedMs / sampleCount;
                                double estimatedTotalMs = avgPerRunMs * totalCount;
                                estimatedTotalSeconds = estimatedTotalMs / 1000.0;
                            }
                        }

                        reportResult?.Invoke(
                            (fallbackDirections, survivors, fineOffset),
                            diameter,
                            elapsedMs);

                        reportProgress?.Invoke(
                            doneCount,
                            totalCount,
                            estimatedTotalSeconds);
                    }
                }
            }
        }
    }
}