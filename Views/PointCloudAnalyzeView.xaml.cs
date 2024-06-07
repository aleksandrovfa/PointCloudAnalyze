using PointCloudAnalyze.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PointCloudAnalyze.Views
{
    /// <summary>
    /// Логика взаимодействия для PointCloudAnalyzeView.xaml
    /// </summary>
    public partial class PointCloudAnalyzeView : Window
    {
        public PointCloudAnalyzeView()
        {

            InitializeComponent();
            Debug.WriteLine("InitializeComponent MainView");
            PointCloudAnalyzeViewModel vm = new PointCloudAnalyzeViewModel();
            vm.CloseRequest += (s, e) => this.Close();
            DataContext = vm;

        }
    }
}
