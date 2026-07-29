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
        public static BenchmarkConfig Config { get; } = new BenchmarkConfig();

        /// <summary>
        /// Runs a benchmark of the WirePackingSolver with varying parameters and reports progress and results.
        /// </summary>
        /// <param name="inputFilePath">The path to the input file.</param>
        /// <param name="orderLabel">The label for the insertion order.</param>
        /// <param name="reportProgress">The action to report progress.</param>
        /// <param name="reportResult">The action to report results.</param>
        /// <exception cref="ArgumentException">Thrown when the input file path is invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the input file is not found.</exception>
        public static void RunSolverBenchmark(
            string inputFilePath,
            string orderLabel,
            Action<int, int>? reportProgress,
            Action<(int fallbackDirections, int survivors, double fineOffset), double, long>? reportResult)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path must not be empty.");

            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException($"Input file not found: {inputFilePath}");

            InputParser parser = new InputParser();
            WirePackingSolver solver = new WirePackingSolver();

            InputData inputData = parser.LoadFromFile(inputFilePath);

            int fallbackCount = ((Config.FallbackDirectionLimit - Config.FallbackDirectionStart) / Config.FallbackDirectionStep) + 1;
            int fineOffsetCount = (int)Math.Floor((Config.MaxFineAngularOffset - Config.MinFineAngularOffset) / Config.FineAngularOffsetStep) + 1;
            int k = Math.Clamp(((Config.SurvivorCountLimit - 1 - Config.FallbackDirectionStart) / Config.FallbackDirectionStep) + 1, 0, fallbackCount);
            int arithmeticSum = k * (2 * Config.FallbackDirectionStart + (k - 1) * Config.FallbackDirectionStep) / 2;
            int survivorSum = arithmeticSum + (fallbackCount - k) * Config.SurvivorCountLimit;
            int totalCount = survivorSum * fineOffsetCount;

            int doneCount = 0;
            int sampleCount = Math.Min(100, totalCount);
            long sampleElapsedMs = 0;
            double estimatedTotalSeconds = 0.0;

            for (int fallbackDirections = Config.FallbackDirectionStart; fallbackDirections <= Config.FallbackDirectionLimit; fallbackDirections += Config.FallbackDirectionStep)
            {
                int maxSurvivorsForThisFallback = Math.Min(Config.SurvivorCountLimit, fallbackDirections);

                for (int survivors = 1; survivors <= maxSurvivorsForThisFallback; survivors++)
                {
                    for (double fineOffset = Config.MinFineAngularOffset; fineOffset <= Config.MaxFineAngularOffset; fineOffset += Config.FineAngularOffsetStep)
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
                            totalCount);
                    }
                }
            }
        }
    }
}