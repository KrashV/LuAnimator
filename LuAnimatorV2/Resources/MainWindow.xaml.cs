using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace LuAnimatorV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<ImageList> paths = new ObservableCollection<ImageList>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void HorizontalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer s = (ScrollViewer)sender;

            if (e.Delta == 0)
                return;

            if (e.Delta > 0)
                s.LineLeft();
            else
                s.LineRight();

            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (new CropWindow()).Show();
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
        }
        
        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && ListBoxFrames.SelectedItem != null)
            {
                ListBoxFrames.Items.Remove(ListBoxFrames.SelectedItem);
            }
        }

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string path in files)
                {
                    Image img = new Image();
                    
                    BitmapImage bimg = new BitmapImage(new Uri(path));

                    img.Source = bimg;
                    ListBoxFrames.Items.Add(bimg);
                }
            }
        }
    }
}