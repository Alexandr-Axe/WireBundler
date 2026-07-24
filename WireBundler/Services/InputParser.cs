using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using WireBundler.Models;

namespace WireBundler.Services
{
    /// <summary>
    /// Parses input data from a text file and loads it into an InputData object.
    /// </summary>
    public class InputParser
    {
        /// <summary>
        /// Loads input data from a specified text file and returns an InputData object containing the parsed radii values.
        /// </summary>
        /// <param name="filePath">The path to the input text file.</param>
        /// <returns>An InputData object containing the parsed wire radii.</returns>
        /// <exception cref="ArgumentException">Thrown when the file is empty or contains no valid radius values.</exception>
        /// <exception cref="FormatException">Thrown when a line cannot be parsed as a valid number.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a radius is zero or negative.</exception>
        public InputData LoadFromFile(string filePath)
        {
            AppLog.Write(LogLevel.INF, $"Loading input file: {filePath}");

            InputData inputData = new();
            string[] lines = File.ReadAllLines(filePath);
            string trimmedLine = string.Empty;
            int lineIndex = 0;

            AppLog.Write(LogLevel.DEB, $"Read {lines.Length} lines from input file.");

            if (lines.Length <= 0)
            {
                AppLog.Write(LogLevel.ERR, "Input file is empty.");
                throw new ArgumentException("File is empty");
            }

            foreach (string line in lines)
            {
                lineIndex++;
                trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    AppLog.Write(LogLevel.DEB, $"Skipped empty line at line {lineIndex}.");
                    continue;
                }

                if (trimmedLine.StartsWith("#"))
                {
                    AppLog.Write(LogLevel.DEB, $"Skipped comment line at line {lineIndex}: {trimmedLine}");
                    continue;
                }

                if (!double.TryParse(trimmedLine, NumberStyles.Float, CultureInfo.InvariantCulture, out double radius))
                {
                    AppLog.Write(LogLevel.ERR, $"Invalid radius value at line {lineIndex}: {trimmedLine}");
                    throw new FormatException($"Invalid radius value: {trimmedLine}");
                }

                if(radius <= 0)
                {
                    AppLog.Write(LogLevel.ERR, $"Non-positive radius at line {lineIndex}: {trimmedLine}");
                    throw new ArgumentOutOfRangeException($"Radius must be positive: {trimmedLine}");
                }

                inputData.Radii.Add(radius);
                AppLog.Write(LogLevel.DEB, $"Parsed radius at line {lineIndex}: {radius:F2}");
            }

            if (inputData.Radii.Count == 0)
            {
                AppLog.Write(LogLevel.ERR, "No valid radius values found in the input file.");
                throw new ArgumentException("No valid radius values found in the file");
            }

            AppLog.Write(LogLevel.INF, $"Input parsing completed successfully. Parsed {inputData.Radii.Count} radii.");
            return inputData;
        }
    }
}
