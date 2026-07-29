using System;
using System.Collections.Generic;
using System.Text;

namespace WireBundler.Models
{
    /// <summary>
    /// Represents the placement of a wire in the bundle, including its radius and position (X, Y).
    /// </summary>
    public class WirePlacement
    {
        public double Radius { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the number of tangents associated with this wire placement.
        /// </summary>
        public int TangentCount { get; set; } = 0;
    }
}
