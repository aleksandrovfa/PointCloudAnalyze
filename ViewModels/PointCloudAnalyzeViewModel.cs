using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using PointCloudAnalyze.Core;
using PointCloudAnalyze.Properties;
using PointCloudAnalyze.ViewModels.Utils;
using PointCloudAnalyze.Models;
using RevitGeometryExporter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Settings = PointCloudAnalyze.Properties.Settings;
using Transform = Autodesk.Revit.DB.Transform;

namespace PointCloudAnalyze.ViewModels
{
    public class PointCloudAnalyzeViewModel : ViewModelBase
    {

        private ICommand _startCommand;
        public ICommand StartCommand => _startCommand ?? (_startCommand = new RelayCommand(param => Start()));

        public double Step 
        {
            get
            {
                return Settings.Default.Step;
            }
            set
            {
                Settings.Default.Step = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(Step));
            }
        }

        public double StepRevit { get => Step / 304.8; }

        public double CountPointInSplitBox { get; set; } = 5;

        public PointCloudAnalyzeViewModel()
        {
         
        }

        private void Start()
        {
            ExportGeometryToXml.FolderName = ((RbsLogger.Logger)Debug.Listeners[0]).filePath2;
            ExportGeometryToXml.ExportUnits = ExportUnits.Mm;


            if (!(RevitApi.Doc.ActiveView is View3D))
            {
                MessageBox.Show("Перейдите на 3D вид");
                return;
            }

            BoundingBoxXYZ bb = (RevitApi.Doc.ActiveView as View3D).GetSectionBox();


            if (bb == null)
            {
                MessageBox.Show("Не найдены границы 3Dв ида");
                return;
            }

            Floor floor = bb.GetElementFromBoundingBox<Floor>();
            if (floor == null)
            {
                MessageBox.Show("Перекрытие не найдено");
                return;
            }

            PointCloudInstance cloud = bb.GetElementFromBoundingBox<PointCloudInstance>();
            if (cloud == null)
            {
                MessageBox.Show("Облако точек не найдено");
                return;
            }
           
            CloudToFloor cloudToFloor = new CloudToFloor(cloud, floor, bb, StepRevit); 

            Transaction tr = new Transaction(RevitApi.Doc, "CloudToFloor");
            tr.Start();

            cloudToFloor.Start();

            tr.Commit();
        }
    }
}
