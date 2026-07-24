using System;
using System.Collections.Generic;
using System.Text;

namespace WireBundler.Models
{
    /// <summary>
    /// Represents the result of the wire bundling process, including the placements of wires and the overall bundle radius.
    /// </summary>
    public class BundleResult
    {
        public List<WirePlacement> Wires { get; set; } = new();
        public double BundleRadius { get; set; }
        public double BundleDiameter => BundleRadius * 2.0;
    }
}
