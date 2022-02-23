using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.GraphicsInterface;
using Teigha.Geometry;
using Teigha.DatabaseServices;

namespace GraphQl_Client.Model
{
    public class BoxJigZ : DrawJig
    {
        public double dZ;
        public Point3d Point1, Point2, Point3, Point4, Point11, Point21, Point31, Point41, Point, basePoint;
        Vector3d axisX;
        Matrix3d ucs;

        public BoxJigZ(Point3d point1, Point3d point2, Point3d point3, Point3d point4)
        {
            Point1 = point1;
            Point2 = point2;
            Point3 = point3;
            Point4 = point4;
            Point11 = point1;
            Point21 = point2;
            Point31 = point3;
            Point41 = point4;
            ucs = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;
            CoordinateSystem3d cs = ucs.CoordinateSystem3d;
            axisX = cs.Xaxis;
            basePoint = cs.Origin;

        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions jppo = new JigPromptPointOptions();
            jppo.Message = "Грань противоположная основанию:";
            PromptPointResult ppr = prompts.AcquirePoint(jppo);
            if (ppr.Status == PromptStatus.OK)
            {
                if (ppr.Value == basePoint)
                {
                    return SamplerStatus.NoChange;
                }
                else
                {
                    Point = ppr.Value;
                    return SamplerStatus.OK;
                }
            }
            return SamplerStatus.Cancel;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            this.dZ = basePoint.GetVectorTo(Point).DotProduct(axisX);

            Point11 = Point1.Add(axisX.MultiplyBy(this.dZ));
            Point21 = Point2.Add(axisX.MultiplyBy(this.dZ));
            Point31 = Point3.Add(axisX.MultiplyBy(this.dZ));
            Point41 = Point4.Add(axisX.MultiplyBy(this.dZ));

            draw.Geometry.Draw(new Line(Point1, Point2));
            draw.Geometry.Draw(new Line(Point2, Point3));
            draw.Geometry.Draw(new Line(Point3, Point4));
            draw.Geometry.Draw(new Line(Point4, Point1));

            draw.Geometry.Draw(new Line(Point1, Point11));
            draw.Geometry.Draw(new Line(Point2, Point21));
            draw.Geometry.Draw(new Line(Point3, Point31));
            draw.Geometry.Draw(new Line(Point4, Point41));

            draw.Geometry.Draw(new Line(Point11, Point21));
            draw.Geometry.Draw(new Line(Point21, Point31));
            draw.Geometry.Draw(new Line(Point31, Point41));
            draw.Geometry.Draw(new Line(Point41, Point11));

            return false;
        }
    }
}
