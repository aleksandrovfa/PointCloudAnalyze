using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using RevitGeometryExporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Transform = Autodesk.Revit.DB.Transform;

namespace PointCloudAnalyze.Models
{
    public class CloudToFloor
    {

        public Floor Floor { get; set; }
        public PointCloudInstance Cloud { get; set; }
        public double StepRevit { get; set; }

        public BoundingBoxXYZ BB { get; set; }
        public CloudToFloor(PointCloudInstance cloud, Floor floor, BoundingBoxXYZ bb, double step)
        {
            Cloud = cloud;
            Floor = floor;
            StepRevit = step;
            BB = bb;
        }

        public void Start()
        {


            List<BoundingBoxXYZ> bbs = SplitBoundingBox(BB, StepRevit);

            if (bbs.Count() > 200)
            {
                var result = MessageBox.Show($"Ограничивающая рамка разбилась на {bbs.Count} частей. " +
                    $"Анализ может занять длительное время. \n\n" +
                    $"Вы уверены что хотите продолжить?", "Проблемка.", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }



            List<XYZ> points = GetPointsInside(StepRevit, Cloud, BB);

            ExportGeometryToXml.ExportPoints(points, "p");


            SlabShapeEditor editor = Floor.SlabShapeEditor;

            foreach (SlabShapeVertex vertex in Floor.SlabShapeEditor.SlabShapeVertices)
            {
                if (vertex.VertexType == SlabShapeVertexType.Corner)
                {
                    XYZ p = vertex.Position;
                    if (BB.Contains(p))
                    {
                        //if (!points.Contains(p, new ComparePoint()))
                        //{
                        double offset = GetOffsetPointSketch(p, Cloud, BB);
                        if (offset != 0)
                        {
                            editor.ModifySubElement(vertex, offset);
                        }
                        // }
                    }
                }
            }

            foreach (XYZ point in points)
            {
                editor.DrawPoint(point);
            }

        }

        private static List<XYZ> GetPointsInside(double step, PointCloudInstance cloud, BoundingBoxXYZ bb)
        {
            List<XYZ> points = new List<XYZ>();

            ExportGeometryToXml.ExportSolid(bb.CreateSolidFromBoundingBox(), "Source");

            List<BoundingBoxXYZ> bbs = SplitBoundingBox(bb, step);

            foreach (var bbSplit in bbs)
            {
                XYZ p = GetPoint(bbSplit, cloud);
                ExportGeometryToXml.ExportSolid(bbSplit.CreateSolidFromBoundingBox(), "SplitSource");
                if (p != null)
                {
                    points.Add(p);
                }
            }

            return points;
        }


        private double GetOffsetPointSketch(XYZ p, PointCloudInstance cloud, BoundingBoxXYZ bb)
        {
            BoundingBoxXYZ newbb = bb.SetTransform();

            BoundingBoxXYZ bbpoint = new BoundingBoxXYZ();

            XYZ max = new XYZ(p.X + 500 / 304.8, p.Y + 500 / 304.8, newbb.Max.Z);
            XYZ min = new XYZ(p.X - 500 / 304.8, p.Y - 500 / 304.8, newbb.Min.Z);

            bbpoint.Max = max;
            bbpoint.Min = min;
            ExportGeometryToXml.ExportSolid(bbpoint.CreateSolidFromBoundingBox(), "sketchP");

            XYZ result = GetPoint(bbpoint, cloud);

            if (result != null)
            {
                return Math.Round(Math.Abs(result.Z - p.Z),3);
            }
            else
            {
                return 0;
            }
        }

       


        public static List<BoundingBoxXYZ> SplitBoundingBox(BoundingBoxXYZ bb, double step)
        {
            Transform transform = bb.Transform;


            BoundingBoxXYZ newbb = new BoundingBoxXYZ();
            newbb.Max = transform.OfPoint(bb.Max);
            newbb.Min = transform.OfPoint(bb.Min);

            List<BoundingBoxXYZ> result = new List<BoundingBoxXYZ>();

            double minX = newbb.Min.X;
            double minY = newbb.Min.Y;
            double maxX = newbb.Max.X;
            double maxY = newbb.Max.Y;

            for (double x = minX; x < maxX; x += step)
            {
                for (double y = minY; y < maxY; y += step)
                {
                    BoundingBoxXYZ subBox = new BoundingBoxXYZ();
                    subBox.Min = new XYZ(x, y, newbb.Min.Z);
                    subBox.Max = new XYZ(Math.Min(x + step, maxX), Math.Min(y + step, maxY), newbb.Max.Z);
                    result.Add(subBox);
                }
            }

            return result;
        }


        public static XYZ GetPoint(BoundingBoxXYZ bb, PointCloudInstance cloud)
        {
            List<Plane> planes = new List<Plane>
            {

                Plane.CreateByNormalAndOrigin(XYZ.BasisX,bb.Min),
                Plane.CreateByNormalAndOrigin(-XYZ.BasisX,bb.Max),
                Plane.CreateByNormalAndOrigin(XYZ.BasisY,bb.Min),
                Plane.CreateByNormalAndOrigin(-XYZ.BasisY,bb.Max),
                Plane.CreateByNormalAndOrigin(XYZ.BasisZ,bb.Min),
                Plane.CreateByNormalAndOrigin(-XYZ.BasisZ,bb.Max)


            };

            PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);
            List<XYZ> points = cloud
                      .GetPoints(filter, 1, 20)
                      .Select(xo => (XYZ)xo)
                      .ToList();

            if (points.Count > 0)
            {
                Transform transform = cloud.GetTransform();

                ExportGeometryToXml.ExportPoints(points.Select(xf => transform.OfPoint(xf)), "psplit");

                double z = points.Sum(xu => xu.Z) / points.Count();
                double x = points.Sum(xu => xu.X) / points.Count();
                double y = points.Sum(xu => xu.Y) / points.Count();

                XYZ p = new XYZ(x, y, z);
                //XYZ p = new XYZ(x, y, z) + transform.Origin;
                p = transform.OfPoint(p);

                return p;
            }
            else
            {
                return null;
            }
        }


    }
}
