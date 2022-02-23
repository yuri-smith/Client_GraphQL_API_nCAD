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
    public class BoxJigXY : DrawJig
    {
        public double dX, dY;
        public Point3d Point1, Point2, Point3, Point4;
        Vector3d diag, axisX, axisY;
        Matrix3d ucs;

        public BoxJigXY(Point3d point)
        {
            Point1 = point;
            Point3 = point;
            ucs = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;
            axisX = ucs.CoordinateSystem3d.Xaxis;
            axisY = ucs.CoordinateSystem3d.Yaxis;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions jppo = new JigPromptPointOptions();
            jppo.Message = "Противоположный диагональный угол основания:";

            PromptPointResult ppr = prompts.AcquirePoint(jppo);
            if (ppr.Status == PromptStatus.OK)
            {
                if (ppr.Value == Point1)
                {
                    return SamplerStatus.NoChange;
                }
                else
                {
                    Point3 = ppr.Value;
                    return SamplerStatus.OK;
                }
            }
            return SamplerStatus.Cancel;
        }
        protected override bool WorldDraw(WorldDraw draw)
        {
            draw.Geometry.Draw(new Line(Point1, Point3));

            diag = Point1.GetVectorTo(Point3);
            dX = diag.DotProduct(axisX);
            dY = diag.DotProduct(axisY);

            this.Point2 = Point1.Add(axisX.MultiplyBy(dX));
            this.Point4 = Point1.Add(axisY.MultiplyBy(dY));

            draw.Geometry.Draw(new Line(Point1, Point2));
            draw.Geometry.Draw(new Line(Point2, Point3));
            draw.Geometry.Draw(new Line(Point3, Point4));
            draw.Geometry.Draw(new Line(Point4, Point1));

            return false;
        }
    }
}
