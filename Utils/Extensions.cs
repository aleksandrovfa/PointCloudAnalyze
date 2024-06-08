using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using PointCloudAnalyze.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PointCloudAnalyze.Models
{
    public static class Extensions
    {
        public static Solid CreateSolidFromBoundingBox(this BoundingBoxXYZ bbox)
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


            Solid preTransformBox = GeometryCreationUtilities
              .CreateExtrusionGeometry(loopList, XYZ.BasisZ,
                height);

            Solid transformBox = SolidUtils.CreateTransformed(preTransformBox, bbox.Transform);

            return transformBox;
        }

        public static BoundingBoxXYZ SetTransform(this BoundingBoxXYZ bb)
        {
            Transform transform = bb.Transform;
            BoundingBoxXYZ newbb = new BoundingBoxXYZ();
            newbb.Max = transform.OfPoint(bb.Max);
            newbb.Min = transform.OfPoint(bb.Min);
            return newbb;
        }

        public static T GetElementFromBoundingBox<T>(this BoundingBoxXYZ bb) where T : Element
        {
            BoundingBoxXYZ newbb = bb.SetTransform();
            Outline outline = new Outline(newbb.Min, newbb.Max);

            List<T> result = new FilteredElementCollector(RevitApi.Doc)
                 .WhereElementIsNotElementType()
                 .WherePasses(new BoundingBoxIntersectsFilter(outline))
                 .OfClass(typeof(T))
                 .Cast<T>()
                 .ToList();

            if (result.Count == 0)
                return null;

            if (result.Count == 1)
                return result[0];

            if (result.Count > 1)
            {
                var messageResult = MessageBox.Show($"В ограничевающую рамку попало больше одного элемента категории ({result[0].Category.Name}) \n" +
                    $"Выбрать элемент вручную?", "Проблемка", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (messageResult == MessageBoxResult.OK)
                {
                    T elem = GetElementFromPick<T>();
                    return elem;
                }
            }
            return null;
        }


        public static bool Contains(this BoundingBoxXYZ bb, XYZ p)
        {
            BoundingBoxXYZ newbb = bb.SetTransform();

            if (newbb.Min.X < p.X && newbb.Min.Y < p.Y)
            {
                if (newbb.Max.X > p.X && newbb.Max.Y > p.Y)
                {
                    return true;
                }
            }
            return false;
        }


        public static T GetElementFromPick<T>() where T : Element
        {
            Reference refer = RevitApi.DocUI.Selection.PickObject(ObjectType.Element, new SelectionFilter<T>(), "Выберите элемент");
            T elem = RevitApi.Doc.GetElement(refer.ElementId) as T;
            return elem;
        }
    }


    public class SelectionFilter<T> : ISelectionFilter where T : Element
    {
        public bool AllowElement(Element elem)
        {
            return elem is T;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
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
