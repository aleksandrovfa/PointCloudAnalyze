using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using PointCloudAnalyze.Core;
using PointCloudAnalyze.Properties;
using PointCloudAnalyze.ViewModels.Utils;
using PointCloudAnalyze.Models;
//using RevitGeometryExporter;
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
using Revit.Async;

namespace PointCloudAnalyze.ViewModels
{
    public class PointCloudAnalyzeViewModel : ViewModelBase
    {

        private ICommand _startCommand;
        public ICommand StartCommand => _startCommand ?? (_startCommand = new RelayCommand(param => Start()));

        public bool InsideCreate
        {
            get
            {
                return Settings.Default.InsideCreate;
            }
            set
            {
                Settings.Default.InsideCreate = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(InsideCreate));
            }
        }
        public int InsideStep
        {
            get
            {
                return Settings.Default.InsideStep;
            }
            set
            {
                Settings.Default.InsideStep = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(InsideStep));
            }
        }
        public int InsidePointCount
        {
            get
            {
                return Settings.Default.InsidePointCount;
            }
            set
            {
                Settings.Default.InsidePointCount = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(InsidePointCount));
            }
        }
        public bool InsideExactly
        {
            get
            {
                return Settings.Default.InsideExactly;
            }
            set
            {
                Settings.Default.InsideExactly = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(InsideExactly));
            }
        }
        public bool BorderCreate
        {
            get
            {
                return Settings.Default.BorderCreate;
            }
            set
            {
                Settings.Default.BorderCreate = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(BorderCreate));
            }
        }
        public int BorderSizeBox
        {
            get
            {
                return Settings.Default.BorderSizeBox;
            }
            set
            {
                Settings.Default.BorderSizeBox = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(BorderSizeBox));
            }
        }
        public int BorderPointCount
        {
            get
            {
                return Settings.Default.BorderPointCount;
            }
            set
            {
                Settings.Default.BorderPointCount = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(BorderPointCount));
            }
        }

        public PointCloudAnalyzeViewModel()
        {

        }

        private async void Start()
        {
            try
            {

                await RevitTask.RunAsync(app =>
                {
                    //ExportGeometryToXml.FolderName = ((RbsLogger.Logger)Debug.Listeners[0]).filePath2;
                    //ExportGeometryToXml.ExportUnits = ExportUnits.Mm;


                    if (!(RevitApi.Doc.ActiveView is View3D))
                    {
                        MessageBox.Show("Перейдите на 3D вид");
                        return;
                    }


                    if (!(RevitApi.Doc.ActiveView as View3D).IsSectionBoxActive)
                    {
                        MessageBox.Show("Не найдены границы 3D вида");
                        return;
                    }

                    BoundingBoxXYZ bb = (RevitApi.Doc.ActiveView as View3D).GetSectionBox();


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

                    CloudToFloor cloudToFloor = new CloudToFloor(cloud, floor, bb);

                    Transaction tr = new Transaction(RevitApi.Doc, "CloudToFloor");
                    tr.Start();
                    if (InsideCreate)
                    {
                        cloudToFloor.StartInside();
                    }
                    if (BorderCreate)
                    {
                        cloudToFloor.StartBorder();
                    }
                   

                    tr.Commit();

                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Что то поломалось... Скоро починим");
            }

           
        }
    }
}
