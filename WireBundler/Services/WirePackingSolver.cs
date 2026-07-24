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

        /// <summary>
        /// Solves the wire packing problem for the given input data.
        /// </summary>
        /// <param name="inputData">Input data containing all wire radii.</param>
        /// <returns>A bundle result containing wire positions and bundle radius.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the input is null or contains no radii.
        /// </exception>
        public BundleResult Solve(InputData inputData)
        {
            if (inputData == null || inputData.Radii.Count == 0)
                throw new ArgumentException("Input data is null or empty");

            List<double> sortedRadii = inputData.Radii
                .OrderByDescending(r => r)
                .ToList();

            BundleResult result = new();

            foreach (double newWireRadius in sortedRadii)
            {
                WirePlacement newPlacement = CreateWirePlacement(result.Wires, newWireRadius);
                result.Wires.Add(newPlacement);
            }

            result.BundleRadius = CalculateBundleRadius(result.Wires);

            return result;
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
        private double CalculateBundleRadius(List<WirePlacement> placedWires, WirePlacement candidateWire)
        {
            double currentBundleRadius = CalculateBundleRadius(placedWires);

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
                return new WirePlacement
                {
                    Radius = newWireRadius,
                    X = 0,
                    Y = 0
                };
            }

            if (alreadyPlacedWires.Count == 1)
            {
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

            foreach (WirePlacement placedWire in alreadyPlacedWires)
            {
                allCandidatePlacements.AddRange(
                    GetFallbackPlacementsAroundOneWire(placedWire, newWireRadius));
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

            List<WirePlacement> validCandidatePlacements = allCandidatePlacements
                .Where(candidatePlacement =>
                    !DoesOverlapWithAnyPlacedWire(candidatePlacement, alreadyPlacedWires))
                .ToList();

            if (validCandidatePlacements.Count == 0)
                throw new InvalidOperationException("No valid placement found for the next wire.");

            WirePlacement bestPlacement = validCandidatePlacements[0];
            double smallestBundleRadius = CalculateBundleRadius(alreadyPlacedWires, bestPlacement);

            for (int candidateIndex = 1; candidateIndex < validCandidatePlacements.Count; candidateIndex++)
            {
                WirePlacement currentCandidate = validCandidatePlacements[candidateIndex];
                double currentCandidateBundleRadius =
                    CalculateBundleRadius(alreadyPlacedWires, currentCandidate);

                if (currentCandidateBundleRadius < smallestBundleRadius)
                {
                    smallestBundleRadius = currentCandidateBundleRadius;
                    bestPlacement = currentCandidate;
                }
            }

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
        private IEnumerable<WirePlacement> GetFallbackPlacementsAroundOneWire(WirePlacement placedWire, double newWireRadius)
        {
            List<WirePlacement> fallbackPlacements = new();

            double distanceBetweenCenters = placedWire.Radius + newWireRadius;

            fallbackPlacements.Add(new WirePlacement
            {
                Radius = newWireRadius,
                X = placedWire.X + distanceBetweenCenters,
                Y = placedWire.Y
            });

            return fallbackPlacements;
        }
    }
}
