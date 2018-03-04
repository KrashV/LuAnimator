using System;
using System.Threading;
using System.Windows;

namespace LuAnimatorV2
{
    public partial class ProgressBarTaskOnUiThread : Window
    {
        public ProgressBarTaskOnUiThread(string title)
        {
            InitializeComponent();

            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            this.progressTitle.Content = title;
        }
    }
}