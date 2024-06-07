using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace PointCloudAnalyze.Core
{
    public static class RevitApi
    {
        private static ExternalCommandData _commandData = null;
        private static UIDocument _docUI = null;
        private static UIApplication _appUI = null;
        private static Application _app = null;
        private static Document _doc = null;
        private static string _docName = null;
        public static List<RevitLinkInstance> _revitLinkInstanceAll = null;
        public static List<Document> _docsLink = null;
        public static List<Document> _docsAll = null;
        public static List<string> _docsNameAll = null;



        public static ExternalCommandData CommandData
        {
            get
            {
                return _commandData;
            }
            set
            {
                _commandData = value;
                _appUI = _commandData.Application;
                _docUI = _appUI.ActiveUIDocument;
                _doc = _docUI.Document;
                _docName = _doc.Title;
                _app = _appUI.Application;
                _revitLinkInstanceAll = new FilteredElementCollector(_doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .WhereElementIsNotElementType()
                    .Cast<RevitLinkInstance>()
                    .Where(x => x.GetLinkDocument() != null)
                    .ToList();

                _docsLink = _revitLinkInstanceAll
                    .Select(x => x.GetLinkDocument())
                    .ToList();
                _docsAll = new List<Document>();
                _docsLink.ForEach(x => _docsAll.Add(x));
                _docsAll.Add(_doc);
                _docsNameAll = _docsAll.Select(x => x.Title).ToList();

            }
        }
        public static UIDocument DocUI { get { return _docUI; } }
        public static UIApplication AppUI { get { return _appUI; } }
        public static Application App { get { return _app; } }
        public static Document Doc { get { return _doc; } }
        public static string DocName { get { return _docName; } }
        public static List<RevitLinkInstance> RevitLinkInstanceAll { get { return _revitLinkInstanceAll; } }
        public static List<Document> DocsLink { get { return _docsLink; } }
        public static List<Document> DocsAll { get { return _docsAll; } }
        public static List<string> DocsNameAll { get { return _docsNameAll; } }

        public static void Refresh()
        {
            RevitApi.CommandData = RevitApi.CommandData;
        }

    }
}