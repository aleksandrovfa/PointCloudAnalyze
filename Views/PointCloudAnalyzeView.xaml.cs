using PointCloudAnalyze.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        private PointCloudAnalyzeViewModel viewModel;
        public PointCloudAnalyzeView()
        {

            InitializeComponent();

            this.Title = this.Title + $" (ver.{Assembly.GetExecutingAssembly().GetName().Version.ToString()})";
            Debug.WriteLine("InitializeComponent MainView");
            this.Closing += this.CompareMonolithView_Closing;
            viewModel = new PointCloudAnalyzeViewModel();
            viewModel.CloseRequest += (s, e) => this.Close();
            DataContext = viewModel;

        }

        private void CompareMonolithView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PointCloudAnalyzeCmd._view = null;
        }


    }
}
