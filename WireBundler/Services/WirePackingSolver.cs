using System;
using System.Collections.Generic;
using System.Linq;
using WireBundler.Models;

namespace WireBundler.Services
{
    /// <summary>
    /// Solves the wire packing problem by arranging wires of given radii into a bundle with minimal radius.
    /// </summary>
    public class WirePackingSolver
    {
        /// <summary>
        /// Small tolerance used to reduce floating-point comparison errors.
        /// </summary>
        private const double Epsilon = 1e-6;
        public int FallbackDirectionCount { get; }
        public int CoarseSurvivorCount { get; }
        public double FineAngularOffsetDegrees { get; }
        public int MaxCandidateCount { get; }

#if DEBUG
        public WirePackingSolver(int fallbackDirectionCount = 7, int coarseSurvivorCount = 1, double fineAngularOffsetDegrees = 0.0, int maxCandidateCount = 20)
        {
            FallbackDirectionCount = fallbackDirectionCount;
            CoarseSurvivorCount = coarseSurvivorCount;
            FineAngularOffsetDegrees = fineAngularOffsetDegrees;
            MaxCandidateCount = maxCandidateCount;
        }
#else
        public WirePackingSolver()
        {
            FallbackDirectionCount = 4;
            CoarseSurvivorCount = 1;
            FineAngularOffsetDegrees = 0.0;
            MaxCandidateCount = 20;
        }
#endif



        /// <summary>
        /// Solves the wire packing problem for the given input data and specified order of radii (USED FOR BENCHMARK)
        /// </summary>
        /// <param name="inputData">The input data containing the wire radii.</param>
        /// <param name="orderLabel">The label for the insertion order.</param>
        /// <returns>Returns the result of the wire packing solution.</returns>
        /// <exception cref="ArgumentException">Thrown when the input is null or contains no radii.</exception>
        public BundleResult Solve(InputData inputData, string orderLabel)
        {
            if (inputData == null || inputData.Radii.Count == 0)
            {
                AppLog.Write(LogLevel.ERR, "WirePackingSolver.Solve(orderLabel) failed because input data is null or empty.");
                throw new ArgumentException("Input data is null or empty");
            }

            List<double> radii = inputData.Radii.ToList();

            IEnumerable<double> orderedRadii = orderLabel switch
            {
                "DESC" => radii.OrderByDescending(r => r),
                "ASC" => radii.OrderBy(r => r),
                "ALT" => CreateAlternatingOrder(radii),
                _ => radii.OrderByDescending(r => r)
            };

            AppLog.Write(LogLevel.INF,
                $"Wire packing solver started with {radii.Count} input radii using order '{orderLabel}'.");

            return SolveWithOrder(orderedRadii.ToList());
        }

        /// <summary>
        /// Solves the wire packing problem for the given input data.
        /// </summary>
        /// <param name="inputData">Input data containing all wire radii.</param>
        /// <returns>A bundle result containing wire positions and bundle radius.</returns>
        /// <exception cref="ArgumentException">Thrown when the input is null or contains no radii.</exception>
        public BundleResult Solve(InputData inputData)
        {
            if (inputData == null || inputData.Radii.Count == 0)
            {
                AppLog.Write(LogLevel.ERR, "WirePackingSolver.Solve failed because input data is null or empty.");
                throw new ArgumentException("Input data is null or empty");
            }

            AppLog.Write(LogLevel.INF, $"Wire packing solver started with {inputData.Radii.Count} input radii.");

            List<IEnumerable<double>> insertionOrders = new List<IEnumerable<double>>
            {
                inputData.Radii.OrderByDescending(r => r),
                inputData.Radii.OrderBy(r => r),
                CreateAlternatingOrder(inputData.Radii)
            };

            List<BundleResult> allResults = insertionOrders
                .Select(order => SolveWithOrder(order.ToList()))
                .ToList();

            BundleResult bestResult = allResults
                .OrderBy(result => result.BundleRadius)
                .First();

            AppLog.Write(LogLevel.INF, $"Wire packing solver finished. Best bundle diameter {bestResult.BundleDiameter:F2} mm.");

            return bestResult;
        }

        /// <summary>
        /// Solves the wire packing problem using a specific order of radii.
        /// </summary>
        /// <param name="radii">The list of wire radii in the order they should be placed.</param>
        /// <returns>The result of the wire packing solution.</returns>
        private BundleResult SolveWithOrder(List<double> radii)
        {
            BundleResult result = new();

            foreach (double newWireRadius in radii)
            {
                AppLog.Write(LogLevel.DEB, $"Placing wire with radius {newWireRadius:F2}.");

                WirePlacement newPlacement = CreateWirePlacement(result.Wires, newWireRadius);
                result.Wires.Add(newPlacement);

                AppLog.Write(LogLevel.DEB, $"Placed wire: r={newPlacement.Radius:F2}, x={newPlacement.X:F2}, y={newPlacement.Y:F2}");
            }

            RecenterLayout(result.Wires);

            AppLog.Write(LogLevel.INF, "Recentered wire layout before final bundle radius calculation.");
            result.BundleRadius = CalculateBundleRadius(result.Wires);

            AppLog.Write(LogLevel.INF, $"Wire packing solver finished. Bundle diameter: {result.BundleDiameter:F2} mm.");

            return result;
        }

        /// <summary>
        /// Creates an alternating order of radii, starting with the largest, then the smallest, and so on.
        /// </summary>
        /// <param name="radii">The list of wire radii.</param>
        /// <returns>The list of radii in alternating order.</returns>
        private List<double> CreateAlternatingOrder(List<double> radii)
        {
            List<double> sorted = radii
                .OrderByDescending(r => r)
                .ToList();

            List<double> alternating = new List<double>();

            int left = 0;
            int right = sorted.Count - 1;

            while (left < right)
            {
                if (left < right)
                {
                    alternating.Add(sorted[left++]);
                    alternating.Add(sorted[right--]);
                }
            }

            if(left == right)
            {
                alternating.Add(sorted[left]);
            }

            AppLog.Write(LogLevel.DEB, $"Created alternating insertion order: {string.Join("; ", alternating.Select(r => r.ToString("F2")))}");

            return alternating;
        }

        /// <summary>
        /// Calculates the radius of the bundle based on the placed wires.
        /// </summary>
        /// <param name="placedWires">The wires that are already placed.</param>
        /// <returns>The required bundle radius measured from the origin.
        private double CalculateBundleRadius(List<WirePlacement> placedWires)
        {
            double largestRequiredRadius = 0.0;

            foreach (WirePlacement placedWire in placedWires)
            {
                double wireCenterX = placedWire.X;
                double wireCenterY = placedWire.Y;

                double distanceFromBundleCenter = Math.Sqrt(wireCenterX * wireCenterX + wireCenterY * wireCenterY);

                double requiredBundleRadius = distanceFromBundleCenter + placedWire.Radius;

                if (requiredBundleRadius > largestRequiredRadius)
                    largestRequiredRadius = requiredBundleRadius;
            }

            return largestRequiredRadius;
        }

        /// <summary>
        /// Calculates the radius of the bundle if a candidate wire is added to the already placed wires.
        /// </summary>
        /// <param name="placedWires">The wires that are already placed.</param>
        /// <param name="candidateWire">The candidate wire placement being evaluated.</param>
        /// <returns>The required bundle radius after adding the candidate wire.</returns>
        private double CalculateBundleRadius(List<WirePlacement> placedWires, WirePlacement candidateWire, double currentBundleRadius)
        {
            //double currentBundleRadius = CalculateBundleRadius(placedWires);

            double candidateDistanceFromBundleCenter = Math.Sqrt(candidateWire.X * candidateWire.X + candidateWire.Y * candidateWire.Y);

            double candidateRequiredBundleRadius = candidateDistanceFromBundleCenter + candidateWire.Radius;

            return Math.Max(currentBundleRadius, candidateRequiredBundleRadius);
        }

        /// <summary>
        /// Creates a new wire placement based on the already placed wires and the radius of the new wire.
        /// </summary>
        /// <param name="alreadyPlacedWires">The wires that have already been placed.</param>
        /// <param name="newWireRadius">The radius of the new wire.</param>
        /// <returns>The calculated placement of the new wire.</returns>
        private WirePlacement CreateWirePlacement(List<WirePlacement> alreadyPlacedWires, double newWireRadius)
        {
            if (alreadyPlacedWires.Count == 0)
            {
                AppLog.Write(LogLevel.DEB, $"First wire placed at origin with radius {newWireRadius:F2}.");

                return new WirePlacement
                {
                    Radius = newWireRadius,
                    X = 0,
                    Y = 0
                };
            }

            if (alreadyPlacedWires.Count == 1)
            {
                AppLog.Write(LogLevel.DEB, $"Second wire placed next to the first wire with radius {newWireRadius:F2}.");
                return new WirePlacement
                {
                    Radius = newWireRadius,
                    X = alreadyPlacedWires[0].Radius + newWireRadius,
                    Y = 0
                };
            }

            return FindBestWirePlacement(alreadyPlacedWires, newWireRadius);
        }

        /// <summary>
        /// Inserts an item into a bounded top-k list, maintaining the order based on radius and ensuring the list does not exceed the specified maximum count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topK"></param>
        /// <param name="item"></param>
        /// <param name="radius"></param>
        /// <param name="maxCount"></param>
        private static void InsertIntoBoundedTopK<T>(List<(T Item, double Radius)> topK, T item, double radius, int maxCount)
        {
            if (topK.Count < maxCount)
            {
                int insertAt = topK.Count;
                while (insertAt > 0 && radius < topK[insertAt - 1].Radius)
                    insertAt--;
                topK.Insert(insertAt, (item, radius));
            }
            else if (radius < topK[topK.Count - 1].Radius)
            {
                int insertAt = topK.Count - 1;
                while (insertAt > 0 && radius < topK[insertAt - 1].Radius)
                    insertAt--;
                topK.Insert(insertAt, (item, radius));
                topK.RemoveAt(topK.Count - 1);
            }
        }

        /// <summary>
        /// Finds the best placement for a new wire based on the already placed wires and the radius of the new wire.
        /// </summary>
        /// <param name="alreadyPlacedWires">The wires that are already placed.</param>
        /// <param name="newWireRadius">The radius of the new wire.</param>
        /// <returns>The best valid placement for the new wire.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no valid placement can be found.
        /// </exception>
        private WirePlacement FindBestWirePlacement(List<WirePlacement> alreadyPlacedWires, double newWireRadius)
        {
            List<WirePlacement> allCandidatePlacements = new();

            double currentBundleRadius = CalculateBundleRadius(alreadyPlacedWires);

            foreach (WirePlacement placedWire in alreadyPlacedWires)
            {
                allCandidatePlacements.AddRange(
                    GetFallbackPlacementsAroundOneWire(placedWire, newWireRadius, alreadyPlacedWires, currentBundleRadius));
            }

            for (int firstIndex = 0; firstIndex < alreadyPlacedWires.Count; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < alreadyPlacedWires.Count; secondIndex++)
                {
                    WirePlacement firstPlacedWire = alreadyPlacedWires[firstIndex];
                    WirePlacement secondPlacedWire = alreadyPlacedWires[secondIndex];

                    allCandidatePlacements.AddRange(
                        GetPlacementsTangentToTwoWires(
                            firstPlacedWire,
                            secondPlacedWire,
                            newWireRadius));
                }
            }

            AppLog.Write(LogLevel.DEB, $"Generated {allCandidatePlacements.Count} candidate placements for wire radius {newWireRadius:F2}.");

            List<WirePlacement> deduplicatedCandidatePlacements = new();
            HashSet<(long, long)> seenGridKeys = new();

            foreach (WirePlacement candidate in allCandidatePlacements)
            {
                long gridX = (long)Math.Round(candidate.X / Epsilon);
                long gridY = (long)Math.Round(candidate.Y / Epsilon);

                if (seenGridKeys.Add((gridX, gridY)))
                    deduplicatedCandidatePlacements.Add(candidate);            
            }

            AppLog.Write(LogLevel.DEB, $"Deduplicated {allCandidatePlacements.Count} candidates to {deduplicatedCandidatePlacements.Count}.");

            List<WirePlacement> validCandidatePlacements = deduplicatedCandidatePlacements
                .Where(candidatePlacement =>
                    !DoesOverlapWithAnyPlacedWire(candidatePlacement, alreadyPlacedWires))
                .ToList();

            AppLog.Write(LogLevel.DEB, $"Found {validCandidatePlacements.Count} valid candidate placements for wire radius {newWireRadius:F2}.");

            if (validCandidatePlacements.Count == 0)
            {
                AppLog.Write(LogLevel.ERR, $"No valid placement found for wire radius {newWireRadius:F2}.");
                throw new InvalidOperationException("No valid placement found for the next wire.");
            }

            int maxCandidatesToKeep = Math.Max(1, MaxCandidateCount);

            var scoredCandidates = new List<(WirePlacement Placement, double Radius)>(maxCandidatesToKeep);

            foreach (WirePlacement candidate in validCandidatePlacements)
            {
                double radius = CalculateBundleRadius(alreadyPlacedWires, candidate, currentBundleRadius);

                InsertIntoBoundedTopK(scoredCandidates, candidate, radius, maxCandidatesToKeep);
            }

            WirePlacement bestPlacement = scoredCandidates[0].Placement;
            double smallestBundleRadius = scoredCandidates[0].Radius;

            AppLog.Write(LogLevel.DEB,
                $"Best placement selected (after top-k): r={bestPlacement.Radius:F2}, " +
                $"x={bestPlacement.X:F2}, y={bestPlacement.Y:F2}, " +
                $"bundle radius={smallestBundleRadius:F2}");

            return bestPlacement;
        }

        /// <summary>
        /// Checks if a candidate wire placement overlaps with any of the already placed wires.
        /// </summary>
        /// <param name="candidatePlacement">The candidate placement being tested.</param>
        /// <param name="alreadyPlacedWires">The wires that are already placed.</param>
        /// <returns>
        /// True if the candidate overlaps at least one placed wire; otherwise false.
        /// </returns>
        private bool DoesOverlapWithAnyPlacedWire(WirePlacement candidatePlacement, List<WirePlacement> alreadyPlacedWires)
        {
            foreach (WirePlacement placedWire in alreadyPlacedWires)
            {
                double horizontalDistance = candidatePlacement.X - placedWire.X;
                double verticalDistance = candidatePlacement.Y - placedWire.Y;

                double distanceBetweenCentersSquared = horizontalDistance * horizontalDistance + verticalDistance * verticalDistance;

                double minimumAllowedDistance = candidatePlacement.Radius + placedWire.Radius;

                double minimumAllowedDistanceSquared = (minimumAllowedDistance - Epsilon) * (minimumAllowedDistance - Epsilon);

                if (distanceBetweenCentersSquared < minimumAllowedDistanceSquared)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the possible placements for a new wire that is tangent to two already placed wires.
        /// </summary>
        /// <param name="firstPlacedWire">The first already placed wire.</param>
        /// <param name="secondPlacedWire">The second already placed wire.</param>
        /// <param name="newWireRadius">The radius of the new wire.</param>
        /// <returns>An enumeration of possible placements for the new wire.</returns>
        private IEnumerable<WirePlacement> GetPlacementsTangentToTwoWires(WirePlacement firstPlacedWire, WirePlacement secondPlacedWire, double newWireRadius)
        {
            List<WirePlacement> tangentPlacements = new();

            double firstCenterX = firstPlacedWire.X;
            double firstCenterY = firstPlacedWire.Y;
            double secondCenterX = secondPlacedWire.X;
            double secondCenterY = secondPlacedWire.Y;

            double distanceFromNewCenterToFirstWireCenter = firstPlacedWire.Radius + newWireRadius;

            double distanceFromNewCenterToSecondWireCenter = secondPlacedWire.Radius + newWireRadius;

            double horizontalDistanceBetweenExistingCenters = secondCenterX - firstCenterX;
            double verticalDistanceBetweenExistingCenters = secondCenterY - firstCenterY;

            double distanceBetweenWireCenters =
                Math.Sqrt(
                    horizontalDistanceBetweenExistingCenters * horizontalDistanceBetweenExistingCenters +
                    verticalDistanceBetweenExistingCenters * verticalDistanceBetweenExistingCenters);

            if (distanceBetweenWireCenters < Epsilon)
                return tangentPlacements;

            if (distanceBetweenWireCenters > distanceFromNewCenterToFirstWireCenter + distanceFromNewCenterToSecondWireCenter + Epsilon)
                return tangentPlacements;

            if (distanceBetweenWireCenters < Math.Abs(distanceFromNewCenterToFirstWireCenter - distanceFromNewCenterToSecondWireCenter) - Epsilon)
                return tangentPlacements;

            double distanceFromFirstCenterToProjectionPoint =
                (
                    distanceFromNewCenterToFirstWireCenter * distanceFromNewCenterToFirstWireCenter -
                    distanceFromNewCenterToSecondWireCenter * distanceFromNewCenterToSecondWireCenter +
                    distanceBetweenWireCenters * distanceBetweenWireCenters
                ) / (2 * distanceBetweenWireCenters);

            double distanceFromProjectionPointToIntersectionSquared =
                distanceFromNewCenterToFirstWireCenter * distanceFromNewCenterToFirstWireCenter -
                distanceFromFirstCenterToProjectionPoint * distanceFromFirstCenterToProjectionPoint;

            if (distanceFromProjectionPointToIntersectionSquared < -Epsilon)
                return tangentPlacements;

            distanceFromProjectionPointToIntersectionSquared = Math.Max(0, distanceFromProjectionPointToIntersectionSquared);

            double distanceFromProjectionPointToIntersection = Math.Sqrt(distanceFromProjectionPointToIntersectionSquared);

            double projectionPointX = firstCenterX + distanceFromFirstCenterToProjectionPoint * horizontalDistanceBetweenExistingCenters / distanceBetweenWireCenters;

            double projectionPointY = firstCenterY + distanceFromFirstCenterToProjectionPoint * verticalDistanceBetweenExistingCenters / distanceBetweenWireCenters;

            double perpendicularOffsetX =  -verticalDistanceBetweenExistingCenters * (distanceFromProjectionPointToIntersection / distanceBetweenWireCenters);

            double perpendicularOffsetY = horizontalDistanceBetweenExistingCenters * (distanceFromProjectionPointToIntersection / distanceBetweenWireCenters);

            tangentPlacements.Add(new WirePlacement
            {
                Radius = newWireRadius,
                X = projectionPointX + perpendicularOffsetX,
                Y = projectionPointY + perpendicularOffsetY
            });

            if (distanceFromProjectionPointToIntersection > Epsilon)
            {
                tangentPlacements.Add(new WirePlacement
                {
                    Radius = newWireRadius,
                    X = projectionPointX - perpendicularOffsetX,
                    Y = projectionPointY - perpendicularOffsetY
                });
            }

            return tangentPlacements;
        }

        /// <summary>
        /// Calculates the possible placements for a new wire that is tangent to one already placed wire.
        /// </summary>
        /// <param name="placedWire">The wire that is already placed.</param>
        /// <param name="newWireRadius">The radius of the new wire.</param>
        /// <returns>An enumeration of possible placements for the new wire.</returns>
        private IEnumerable<WirePlacement> GetFallbackPlacementsAroundOneWire(WirePlacement placedWire, double newWireRadius, List<WirePlacement> alreadyPlacedWires, double currentBundleRadius)
        {
            if (FallbackDirectionCount < 1)
            {
                AppLog.Write(LogLevel.ERR, "FallbackDirectionCount must be at least 1.");
                throw new InvalidOperationException("FallbackDirectionCount must be at least 1.");
            }

            List<WirePlacement> allFallBackPlacements = new();

            List<double> fallbackAngles = Enumerable
                .Range(0, FallbackDirectionCount)
                .Select(i => 2.0 * Math.PI * i / FallbackDirectionCount)
                .ToList();

            List<WirePlacement> coarseCandidates = GenerateFallbackCandidatesForAngles(placedWire, newWireRadius, fallbackAngles)
                .ToList();

            List<(WirePlacement placement, double radius)> bestCoarse = new(CoarseSurvivorCount);

            foreach (WirePlacement candidate in coarseCandidates)
            {
                double radius = CalculateBundleRadius(alreadyPlacedWires, candidate, currentBundleRadius);

                InsertIntoBoundedTopK(bestCoarse, candidate, radius, CoarseSurvivorCount);
            }

            foreach (var (placement, radius) in bestCoarse)
            {
                double baseAngle = Math.Atan2(
                    placement.Y - placedWire.Y,
                    placement.X - placedWire.X);

                double fineOffsetRadians = FineAngularOffsetDegrees * Math.PI / 180.0;

                List<double> fineAngles = fineOffsetRadians > Epsilon ?
                    new List<double>
                    {
                        baseAngle - fineOffsetRadians,
                        baseAngle,
                        baseAngle + fineOffsetRadians
                    }
                    : new List<double> { baseAngle };

                allFallBackPlacements.AddRange(
                    GenerateFallbackCandidatesForAngles(placedWire, newWireRadius, fineAngles));
            }

            AppLog.Write(LogLevel.DEB, $"Generated {allFallBackPlacements.Count} fallback candidates around one wire.");

            return allFallBackPlacements;
        }

        /// <summary>
        /// Recenters the layout of the placed wires around the origin (0,0).
        /// </summary>
        /// <param name="placedWires">The list of placed wires.</param>
        private void RecenterLayout(List<WirePlacement> placedWires)
        {
            if(placedWires.Count == 0)
                return;

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (WirePlacement wire in placedWires)
            {
                minX = Math.Min(minX, wire.X - wire.Radius);
                maxX = Math.Max(maxX, wire.X + wire.Radius);
                minY = Math.Min(minY, wire.Y - wire.Radius);
                maxY = Math.Max(maxY, wire.Y + wire.Radius);
            }

            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;

            foreach (WirePlacement wire in placedWires)
            {
                wire.X -= centerX;
                wire.Y -= centerY;
            }

            AppLog.Write(LogLevel.DEB, $"Recentered layout around ({centerX:F2}, {centerY:F2}).");
        }

        /// <summary>
        /// Generates fallback candidate placements for a new wire around an already placed wire at specified angles.
        /// </summary>
        /// <param name="placedWire"></param>
        /// <param name="newWireRadius"></param>
        /// <param name="angles"></param>
        /// <returns></returns>
        private IEnumerable<WirePlacement> GenerateFallbackCandidatesForAngles(WirePlacement placedWire, double newWireRadius, List<double> angles)
        {
            double distanceBetweenCenters = placedWire.Radius + newWireRadius;

            foreach (double angle in angles)
            {
                double offsetX = distanceBetweenCenters * Math.Cos(angle);
                double offsetY = distanceBetweenCenters * Math.Sin(angle);

                yield return new WirePlacement
                {
                    Radius = newWireRadius,
                    X = placedWire.X + offsetX,
                    Y = placedWire.Y + offsetY
                };
            }
        }
    }
}