using System.Collections.Generic;
using NUnit.Framework;
using WireBundler.Models;
using WireBundler.Services;

namespace WireBundler.Tests
{
    /// <summary>
    /// Regression tests for <see cref="WirePackingSolver"/>. Prior to this file, all verification
    /// of solver changes was manual (running the app, reading console logs, eyeballing
    /// coordinates). These tests pin a known input to known outputs so future refactors of candidate
    /// generation, deduplication, overlap filtering, tie-breaking, or recentering can be checked
    /// automatically.
    ///
    /// The pinned input/output pair is the 5-wire example worked through step-by-step in
    /// WireSolver-Doc.md, using the descending (DESC) insertion order specifically.
    ///
    /// Written against NUnit conventions (<see cref="TestAttribute"/>, <c>Assert.That</c> with
    /// <c>Is.EqualTo(...).Within(...)</c>). If this project uses a different test framework,
    /// swap the attribute and assertion calls accordingly - the arrangement, action, and expected
    /// values below do not depend on the test framework.
    /// </summary>
    public class WirePackingSolverRegressionTests
    {
        private const double DiameterTolerance = 0.01;
        private const double CoordinateTolerance = 0.01;
        private const double RadiusTolerance = 0.000001;

        /// <summary>
        /// Builds a solver with parameters pinned explicitly, rather than relying on the
        /// constructor's DEBUG/RELEASE conditional defaults (see WirePackingSolver.cs), so this
        /// test's expected values do not depend on which build configuration it runs under.
        /// </summary>
        private static WirePackingSolver CreateSolverWithDocumentedExampleParameters()
        {
            return new WirePackingSolver(
            fallbackDirectionCount: 7,
            coarseSurvivorCount: 1,
            fineAngularOffsetDegrees: 0.0,
            maxCandidateCount: 20);
        }

        private static InputData CreateFiveWireInput()
        {
            return new InputData
            {
                Radii = new List<double> { 10.0, 8.0, 6.0, 5.0, 3.0 }
            };
        }

        [Test]
        public void Solve_FiveWiresDescendingOrder_ProducesKnownBundleDiameter()
        {
            // Arrange
            InputData inputData = CreateFiveWireInput();
            WirePackingSolver solver = CreateSolverWithDocumentedExampleParameters();

            // Act
            BundleResult result = solver.Solve(inputData, "DESC");

            // Assert
            const double expectedDiameter = 36.16;

            Assert.That(result.BundleDiameter, Is.EqualTo(expectedDiameter).Within(DiameterTolerance), "Bundle diameter does not match the documented DESC-order example.");
        }

        [Test]
        public void Solve_FiveWiresDescendingOrder_PlacesEachWireAtItsExpectedFinalCoordinates()
        {
            // Arrange
            InputData inputData = CreateFiveWireInput();
            WirePackingSolver solver = CreateSolverWithDocumentedExampleParameters();

            // Act
            BundleResult result = solver.Solve(inputData, "DESC");

            // Assert
            Assert.That(result.Wires, Has.Count.EqualTo(5), "Expected exactly 5 placed wires.");

            AssertWireAt(result.Wires[0], expectedRadius: 10.0, expectedX: -8.00, expectedY: -1.13);
            AssertWireAt(result.Wires[1], expectedRadius: 8.0, expectedX: 10.00, expectedY: -1.13);
            AssertWireAt(result.Wires[2], expectedRadius: 6.0, expectedX: 2.67, expectedY: 10.79);
            AssertWireAt(result.Wires[3], expectedRadius: 5.0, expectedX: 2.56, expectedY: -11.79);
            AssertWireAt(result.Wires[4], expectedRadius: 3.0, expectedX: -6.28, expectedY: 11.75);
        }

        private static void AssertWireAt(WirePlacement wire, double expectedRadius, double expectedX, double expectedY)
        {
            Assert.That(wire.Radius, Is.EqualTo(expectedRadius).Within(RadiusTolerance), "Wire radius does not match expected value.");

            Assert.That(wire.X, Is.EqualTo(expectedX).Within(CoordinateTolerance), "Wire X coordinate does not match expected value.");

            Assert.That(wire.Y, Is.EqualTo(expectedY).Within(CoordinateTolerance), "Wire Y coordinate does not match expected value.");
        }
    }
}