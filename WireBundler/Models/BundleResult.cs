using System;
using System.Collections.Generic;
using System.Text;

namespace WireBundler.Models
{
    public class BundleResult
    {
        public List<WirePlacement> Wires { get; set; } = new();
        public double BundleRadius { get; set; }
        public double BundleDiameter => BundleRadius * 2.0;
    }
}
