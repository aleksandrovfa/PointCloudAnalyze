using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PointCloudAnalyze.Core;
using PointCloudAnalyze.Views;
using Revit.Async;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;

namespace PointCloudAnalyze
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PointCloudAnalyzeCmd : IExternalCommand
    {
        public static PointCloudAnalyzeView _view;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Debug.Listeners.Clear();
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string name = Assembly.GetExecutingAssembly().GetName().Name;
            Debug.Listeners.Add(new RbsLogger.Logger($"{name}_{version}"));
            RevitApi.CommandData = commandData;

            RevitTask.Initialize(RevitApi.AppUI);

            try
            {
                if (_view == null)
                {
                    Debug.WriteLine(" Start!!! Create!!!");

                    //RevitApi.CommandData = commandData;
                    //RevitTask.Initialize(RevitApi.AppUI);
                    _view = new PointCloudAnalyzeView();

                    WindowInteropHelper helper = new WindowInteropHelper(_view);
                    helper.Owner = commandData.Application.MainWindowHandle;

                    _view.Show();
                }
                else
                {
                    Debug.WriteLine(" Create!!! Activate!!!");
                    _view.Activate();

                }
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + e.StackTrace);
                MessageBox.Show(e.Message + e.StackTrace, "Что то поломалось... Скоро починим");
                return Result.Failed;
            }
        }
    }
}
