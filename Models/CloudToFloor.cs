using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;
//using RevitGeometryExporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Transform = Autodesk.Revit.DB.Transform;
using Settings = PointCloudAnalyze.Properties.Settings;

namespace PointCloudAnalyze.Models
{
    public class CloudToFloor
    {

        public Floor Floor { get; set; }
        public PointCloudInstance Cloud { get; set; }


        public BoundingBoxXYZ BB { get; set; }
        public CloudToFloor(PointCloudInstance cloud, Floor floor, BoundingBoxXYZ bb)
        {
            Cloud = cloud;
            Floor = floor;
            //    StepRevit = step;
            BB = bb;
        }

        public void StartInside()
        {
            List<BoundingBoxXYZ> bbs = BB.SplitBoundingBox(Settings.Default.InsideStep / 304.8);

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



            List<XYZ> points = GetPointsInside(Settings.Default.InsideStep / 304.8, Cloud, BB);

            //  ExportGeometryToXml.ExportPoints(points, "p");

            SlabShapeEditor editor = Floor.SlabShapeEditor;

            foreach (XYZ point in points)
            {
                editor.DrawPoint(point);
            }
        }


        public void StartBorder()
        {
            SlabShapeEditor editor = Floor.SlabShapeEditor;


            foreach (SlabShapeVertex vertex in Floor.SlabShapeEditor.SlabShapeVertices)
            {
                if (vertex.VertexType == SlabShapeVertexType.Corner)
                {
                    XYZ p = vertex.Position;
                    if (BB.Contains(p))
                    {
                        double offset = GetOffsetPointSketch(p, Cloud, BB);
                        if (offset != 0)
                        {
                            editor.ModifySubElement(vertex, offset);
                        }
                    }
                }
            }
        }

        private static List<XYZ> GetPointsInside(double step, PointCloudInstance cloud, BoundingBoxXYZ bb)
        {
            List<XYZ> points = new List<XYZ>();

            // ExportGeometryToXml.ExportSolid(bb.CreateSolidFromBoundingBox(), "Source");

            List<BoundingBoxXYZ> bbs = bb.SplitBoundingBox( step);

            foreach (var bbSplit in bbs)
            {
                XYZ p = GetPointInside(bbSplit, cloud);
                // ExportGeometryToXml.ExportSolid(bbSplit.CreateSolidFromBoundingBox(), "SplitSource");
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

            XYZ max = new XYZ(
                p.X + (Settings.Default.BorderSizeBox / 2) / 304.8, 
                p.Y + (Settings.Default.BorderSizeBox / 2) / 304.8, 
                newbb.Max.Z);
            XYZ min = new XYZ(
                p.X - (Settings.Default.BorderSizeBox/2) / 304.8, 
                p.Y - (Settings.Default.BorderSizeBox / 2) / 304.8, 
                newbb.Min.Z);

            bbpoint.Max = max;
            bbpoint.Min = min;
            //ExportGeometryToXml.ExportSolid(bbpoint.CreateSolidFromBoundingBox(), "sketchP");

            XYZ result = GetPointBorder(bbpoint, cloud);

            if (result != null)
            {
                return Math.Round(Math.Abs(result.Z - p.Z), 3);
            }
            else
            {
                return 0;
            }
        }

        public static XYZ GetPointInside(BoundingBoxXYZ bb, PointCloudInstance cloud)
        {
            List<Plane> planes = bb.GetPlanes();

            PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);
            List<XYZ> points = cloud
                      .GetPoints(filter, 0.001, Settings.Default.InsidePointCount)
                      .Select(xo => (XYZ)xo)
                      .ToList();

            if (points.Count > 0)
            {
                Transform transform = cloud.GetTransform();

                //ExportGeometryToXml.ExportPoints(points.Select(xf => transform.OfPoint(xf)), "psplit");
                //double z = points.Sum(xu => xu.Z) / points.Count();
                //double x = points.Sum(xu => xu.X) / points.Count();
                //double y = points.Sum(xu => xu.Y) / points.Count();

                //double z = points.Min(xu => xu.Z);
                //double x = points.Min(xu => xu.X);
                //double y = points.Min(xu => xu.Y);


                //XYZ p = new XYZ(x, y, z);
                XYZ p = points.OrderByDescending(x => x.Z).First();
                p = transform.OfPoint(p);

                if (Settings.Default.InsideExactly)
                {
                    p = new XYZ((bb.Max.X + bb.Min.X) / 2, (bb.Max.Y + bb.Min.Y) / 2, p.Z);
                }

                return p;
            }
            else
            {
                return null;
            }
        }
        public static XYZ GetPointBorder(BoundingBoxXYZ bb, PointCloudInstance cloud)
        {
            List<Plane> planes = bb.GetPlanes();

            PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);
            List<XYZ> points = cloud
                      .GetPoints(filter, 0.001, Settings.Default.BorderPointCount)
                      .Select(xo => (XYZ)xo)
                      .ToList();

            if (points.Count > 0)
            {
                Transform transform = cloud.GetTransform();

                //ExportGeometryToXml.ExportPoints(points.Select(xf => transform.OfPoint(xf)), "psplit");
                double z = points.Sum(xu => xu.Z) / points.Count(); ;
                double x = points.Sum(xu => xu.X) / points.Count(); ;
                double y = points.Sum(xu => xu.Y) / points.Count();


                XYZ p = new XYZ(x, y, z);
                p = transform.OfPoint(p);
              

                return p;
            }
            else
            {
                return null;
            }
        }




        //public static List<XYZ> GetPoints(BoundingBoxXYZ bb, PointCloudInstance cloud, int count)
        //{
        //    List<Plane> planes = bb.GetPlanes();

        //    PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);
        //    List<XYZ> points = cloud
        //              .GetPoints(filter, 1, count)
        //              .Select(xo => (XYZ)xo)
        //              .ToList();

        //    return points;
        //}

        //public static XYZ GetMiddlePoint(PointCloudInstance cloud, List<XYZ> points)
        //{
        //    Transform transform = cloud.GetTransform();

        //    //ExportGeometryToXml.ExportPoints(points.Select(xf => transform.OfPoint(xf)), "psplit");

        //    double z = points.Sum(xu => xu.Z) / points.Count();
        //    double x = points.Sum(xu => xu.X) / points.Count();
        //    double y = points.Sum(xu => xu.Y) / points.Count();

        //    XYZ p = new XYZ(x, y, z);
        //    //XYZ p = new XYZ(x, y, z) + transform.Origin;
        //    p = transform.OfPoint(p);

        //    return p;
        //}

       

    }
}
