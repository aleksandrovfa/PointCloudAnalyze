using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using PointCloudAnalyze.Core;
using PointCloudAnalyze.ViewModels.Utils;
using RevitGeometryExporter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Transform = Autodesk.Revit.DB.Transform;

namespace PointCloudAnalyze.ViewModels
{
    public class PointCloudAnalyzeViewModel : ViewModelBase
    {
        public PointCloudAnalyzeViewModel()
        {
            ExportGeometryToXml.FolderName = ((RbsLogger.Logger)Debug.Listeners[0]).filePath2;
            ExportGeometryToXml.ExportUnits = ExportUnits.Mm;


            Floor floor = RevitApi.Doc.GetElement(new ElementId(452776)) as Floor;

            BoundingBoxXYZ bb = (RevitApi.Doc.ActiveView as View3D).GetSectionBox();

            PointCloudInstance cloud = RevitApi.Doc.GetElement(new ElementId(452867)) as PointCloudInstance;
            //PointCloudInstance cloud = RevitApi.Doc.GetElement(new ElementId(454749)) as PointCloudInstance;

            List<XYZ> points = GetPointsInside(1000 / 304.8, cloud, bb);


            Transaction tr = new Transaction(RevitApi.Doc, "sdf");
            tr.Start();

            ExportGeometryToXml.ExportPoints(points, "p");


            SlabShapeEditor editor = floor.SlabShapeEditor;

            foreach (SlabShapeVertex vertex in floor.SlabShapeEditor.SlabShapeVertices)
            {
                XYZ p = vertex.Position;
                if (BoundingBoxXyzContains(bb,p))
                {
                    if (!points.Contains(p, new ComparePoint()))
                    {
                        double offset = GetOffsetPointSketch(p, cloud, bb);
                        editor.ModifySubElement(vertex, offset);
                    }
                }
            }

            foreach (XYZ point in points)
            {
                editor.DrawPoint(point);
            }


            tr.Commit();
        }

        public bool BoundingBoxXyzContains(BoundingBoxXYZ bb, XYZ p)
        {
            Transform transform = bb.Transform;
            BoundingBoxXYZ newbb = new BoundingBoxXYZ();
            newbb.Max = transform.OfPoint(bb.Max);
            newbb.Min = transform.OfPoint(bb.Min);

            if (newbb.Min.X < p.X && newbb.Min.Y < p.Y)
            {
                if (newbb.Max.X > p.X && newbb.Max.Y > p.Y)
                {
                    return true;
                }
            }

            return false;
        }

        private double GetOffsetPointSketch(XYZ p, PointCloudInstance cloud, BoundingBoxXYZ bb)
        {
            Transform transform = bb.Transform;
            BoundingBoxXYZ newbb = new BoundingBoxXYZ();
            newbb.Max = transform.OfPoint(bb.Max);
            newbb.Min = transform.OfPoint(bb.Min);

            BoundingBoxXYZ bbpoint = new BoundingBoxXYZ();

            XYZ max = new XYZ(p.X + 200 / 304.8, p.Y + 200 / 304.8, newbb.Max.Z);
            XYZ min = new XYZ(p.X - 200 / 304.8, p.Y - 200 / 304.8, newbb.Min.Z);

            bbpoint.Max = max;
            bbpoint.Min = min;
            ExportGeometryToXml.ExportSolid(CreateSolidFromBoundingBox(bbpoint), "sketchP");

            XYZ result = GetPoint(bbpoint, cloud);

            if (result != null)
            {
                return result.Z - p.Z;
            }
            else
            {
                return 0;
            }
        }

        private List<XYZ> GetPointsInside(double step, PointCloudInstance cloud, BoundingBoxXYZ bb)
        {
            List<XYZ> points = new List<XYZ>();

            ExportGeometryToXml.ExportSolid(CreateSolidFromBoundingBox(bb), "Source");

            foreach (var bbSplit in SplitBoundingBox(bb, step))
            {
                XYZ p = GetPoint(bbSplit, cloud);
                ExportGeometryToXml.ExportSolid(CreateSolidFromBoundingBox(bbSplit), "SplitSource");
                if (p != null)
                {
                    points.Add(p);
                }
            }

            return points;
        }


        public List<BoundingBoxXYZ> SplitBoundingBox(BoundingBoxXYZ bb, double step)
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


        public static Solid CreateSolidFromBoundingBox(BoundingBoxXYZ bbox)
        {
            //BoundingBoxXYZ bbox = inputSolid.GetBoundingBox();

            // Corners in BBox coords

            XYZ pt0 = new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Min.Z);
            XYZ pt1 = new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Min.Z);
            XYZ pt2 = new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Min.Z);
            XYZ pt3 = new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Min.Z);

            // Edges in BBox coords

            Line edge0 = Line.CreateBound(pt0, pt1);
            Line edge1 = Line.CreateBound(pt1, pt2);
            Line edge2 = Line.CreateBound(pt2, pt3);
            Line edge3 = Line.CreateBound(pt3, pt0);

            // Create loop, still in BBox coords

            List<Curve> edges = new List<Curve>();
            edges.Add(edge0);
            edges.Add(edge1);
            edges.Add(edge2);
            edges.Add(edge3);

            double height = bbox.Max.Z - bbox.Min.Z;

            CurveLoop baseLoop = CurveLoop.Create(edges);

            List<CurveLoop> loopList = new List<CurveLoop>();
            loopList.Add(baseLoop);



            //ExportGeometryToXml.ExportCurves(edges, "curve");

            Solid preTransformBox = GeometryCreationUtilities
              .CreateExtrusionGeometry(loopList, XYZ.BasisZ,
                height);

            Solid transformBox = SolidUtils.CreateTransformed(preTransformBox, bbox.Transform);



            return transformBox;
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


        //const double _eps = 1.0e-9;

        //public static bool IsZero(
        //double a,
        //double tolerance)
        //{
        //    return tolerance > Math.Abs(a);
        //}

        //public static bool IsZero(double a)
        //{
        //    return IsZero(a, _eps);
        //}

        //public static bool IsEqual(double a, double b)
        //{
        //    return IsZero(b - a);
        //}

        //public static int Compare(double a, double b)
        //{
        //    return IsEqual(a, b) ? 0 : (a < b ? -1 : 1);
        //}

        //public static int Compare(XYZ p, XYZ q)
        //{
        //    int d = Compare(p.X, q.X);

        //    if (0 == d)
        //    {
        //        d = Compare(p.Y, q.Y);

        //        if (0 == d)
        //        {
        //            d = Compare(p.Z, q.Z);
        //        }
        //    }
        //    return d;
        //}

    }

    public class ComparePoint : IEqualityComparer<XYZ>
    {
        public bool Equals(XYZ x, XYZ y)
        {
            return x == y;
        }

        public int GetHashCode(XYZ obj)
        {
            return obj.X.GetHashCode() ^ obj.Y.GetHashCode();
        }
    }
}
