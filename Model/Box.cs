using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Teigha.Geometry;


namespace GraphQl_Client.Model
{
    public class Box
    {
        public Box() { }
        public Box(CoordinateSystem3d cs, double SizeX, double SizeY, double SizeZ)
        {
            this.cs = cs;
            this.SizeX = SizeX;
            this.SizeY = SizeY;
            this.SizeZ = SizeZ;
        }
        public Box(Point3d Origin, Vector3d XAxis, Vector3d YAxis, double SizeX, double SizeY, double SizeZ)
        {
            cs = new CoordinateSystem3d(Origin, XAxis, YAxis);
        }
        public CoordinateSystem3d cs { get; set; }
        public double SizeX { get; set; }
        public double SizeY { get; set; }
        public double SizeZ { get; set; }
        public string ID { get; set; }

    }
}
