using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQl_Client.Model
{
    public class Extremum
    {
        public Extremum() { }
        public double minX { get; set; }
        public double maxX { get; set; }
        public double minY { get; set; }
        public double maxY { get; set; }
        public double minZ { get; set; }
        public double maxZ { get; set; }
    }
}
