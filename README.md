# WireBundler

A desktop tool for computing the minimal-diameter circular bundle arrangement for a set of wires,
given their radii. Supports parsing wire lists from text files, solving with multiple insertion
strategies, rendering the result, and benchmarking solver configurations.

## Features

- Parses wire radius lists from plain text input files
- Packs circular wires into a bundle while avoiding overlaps
- Tries multiple insertion orders (descending, ascending, alternating) and keeps the best result
- Renders the final bundle visually
- Includes a benchmark harness for tuning solver parameters (Debug builds only)

## Getting started

### Prerequisites

- .NET 10.0 SDK
- Visual Studio 2026 (or Visual Studio 2022 with .NET 10 tooling) or Visual Studio Code with the C# Dev Kit extension
- Windows, since the UI is built with WPF (`net10.0-windows`)

### Build and run

```bash
git clone https://github.com/your-org/WireBundler.git
cd WireBundler
dotnet build
dotnet run --project WireBundler
```

### Input file format

Plain text file, one radius per line, `#` starts a comment line:

```text
# sample list of radii for the Diameter algorithm
# values are in millimeters
10.00
8.00
6.00
5.00
3.00
```

## Usage

1. Launch the application.
2. Load an input file via the main window.
3. The solver computes the bundle and displays the resulting diameter and layout.
4. In Debug builds, open the Benchmark window to sweep solver parameters (`FallbackDirectionCount`,
   `CoarseSurvivorCount`, `FineAngularOffsetDegrees`, `MaxCandidateCount`) across multiple inputs
   and insertion orders, and export results to CSV.

## Project structure

| Folder / File | Purpose |
|---|---|
| `WirePackingSolver.cs` | Core packing algorithm |
| `WirePlacement.cs` | Represents a single placed wire (radius, position) |
| `InputData.cs` | Holds the parsed list of input radii |
| `BundleResult.cs` | Holds the final placements and resulting bundle radius/diameter |
| `InputParser.cs` | Reads and validates input files |
| `BundleRenderer.cs` | Renders the final layout |
| `BENCHMARK.cs`, `BenchmarkConfig.cs` | Benchmark harness (Debug builds only) |
| `Logger.cs`, `AppLog.cs`, `LogLevel.cs` | Logging infrastructure |
| `MainWindow.xaml` / `.xaml.cs` | Main application window |
| `BenchmarkWindow.xaml` / `.xaml.cs` | Benchmark UI (Debug builds only) |

## Algorithm

See [`WireSolver-Doc.md`](WireBundler/WireSolver-Doc.md) for a full explanation of the packing algorithm,
including the candidate generation rules, scoring/tie-break logic, and a step-by-step worked
example using the descending (`DESC`) insertion order.

## Known limitations

- Heuristic solver — not guaranteed to find the globally optimal packing.
- Final centering uses a bounding-box midpoint, not a true minimal-enclosing-circle center.
- `MaxCandidateCount` default (20) has not been benchmarked against alternatives; see the
  algorithm documentation for details.
- Benchmark window and harness are only available in Debug builds.

## Testing

Regression tests pin known inputs to known outputs (bundle diameter and final wire coordinates)
so future refactors can be verified automatically. See `WirePackingSolverRegressionTests.cs`.

```bash
dotnet test
```

## Contributing

1. Fork the repository and create a feature branch.
2. Make your changes, keeping the solver's scoring behavior unchanged unless a concrete bug is
   found and documented.
3. Run the existing regression tests and the benchmark suite before submitting changes that touch
   `WirePackingSolver.cs`.
4. Open a pull request describing what changed and why.
