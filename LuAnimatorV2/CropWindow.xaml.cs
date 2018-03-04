using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LuAnimatorV2
{
    /// <summary>
    /// Interaction logic for CropWindow.xaml
    /// </summary>
    public partial class CropWindow : Window
    {
        private Point Dimensions
        {
            get
            {
                int x, y;

                if (!int.TryParse(TextBoxHorizontal.Text, out x))
                    x = 16;
                if (!int.TryParse(TextBoxVertical.Text, out y))
                    y = 16;

                return new Point(x, y);
            }
        }

        public CropWindow()
        {
            InitializeComponent();

            CanvasCropRegions.Opacity = SliderTransparency.Value;
        }
        
        private void ButtonSelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.Filter = "Image files|*.png;*.jpg;*.bmp;*.gif";
            ofd.Title = "Select an image.";
            ofd.FileName = "frame";
            bool? ofdResult = ofd.ShowDialog();
            if (!ofdResult.HasValue || !ofdResult.Value)
                return;

            if (ofd.FileName.IndexOf(".gif") == ofd.FileName.Length - 4 && PromptGif())
            {
                ExtractFrames(ofd.FileName);
            }
            else
            {
                SelectImage(ofd.FileName);
                UpdateDimensions();
            }
        }

        BitmapImage image = null;
        string imagePath = null;

        private bool PromptGif()
        {
            MessageBoxResult mbr = MessageBox.Show("This appears to be a gif. Would you like to extract frames instead of cropping the image?", "Warning", MessageBoxButton.YesNo);
            return mbr == MessageBoxResult.Yes;
        }

        private void SelectImage(string path)
        {
            Uri imageUri = new Uri(path);
            BitmapImage bi = new BitmapImage();

            try
            {
                bi.BeginInit();
                bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = imageUri;
                bi.EndInit();
                bi.Freeze();
            }
            catch(NotSupportedException)
            {
                ImagePreview.Source = null;
                image = null;
                imagePath = null;

                MessageBox.Show("The file could not be loaded. Please select a valid image.");
                return;
            }

            image = bi;
            imagePath = path;
            ImagePreview.Source = bi;
            ImagePreview.Width = bi.PixelWidth * 4;
            ImagePreview.Height = bi.PixelHeight * 4;

            BorderPreview.MaxWidth = ImagePreview.Width + 2;
            BorderPreview.MaxHeight = ImagePreview.Height + 2;
        }

        private void ButtonUpdateDimensions_Click(object sender, RoutedEventArgs e)
        {
            UpdateDimensions();
        }

        private void UpdateDimensions(bool silent = false)
        {
            CanvasCropRegions.Children.Clear();

            if (image == null)
            {
                if (!silent)
                    MessageBox.Show("Please select an image first.");
                return;
            }

            Point dimensions = Dimensions;
            if (dimensions.X == 0 || dimensions.Y == 0)
            {
                if (!silent)
                    MessageBox.Show("Please select frame dimensions larger than 0.");
                return;
            }
            if (dimensions.X >= image.Width || dimensions.Y >= image.Height)
            {
                if (!silent)
                    MessageBox.Show("Please select frame dimensions smaller than the image dimensions.");

                if (dimensions.X >= image.Width)
                    dimensions.X = image.Width;

                if (dimensions.Y >= image.Height)
                    dimensions.Y = image.Height;
                
            }

            int w = (int)ImagePreview.Width / 4;
            int h = (int)ImagePreview.Height / 4;

            for (int i = 0; i < (int)Math.Ceiling(w / dimensions.X); i++)
            {
                for (int j = 0; j < (int)Math.Ceiling(h / dimensions.Y); j++)
                {
                    Rectangle r = new Rectangle();

                    // Checkerboard pattern
                    if (i % 2 == j % 2) continue;

                    r.Fill = Brushes.Black;
                    r.Width = dimensions.X * 4;
                    r.Height = dimensions.Y * 4;
                    CanvasCropRegions.Children.Add(r);
                    Canvas.SetLeft(r, i * dimensions.X * 4);
                    Canvas.SetTop(r, j * dimensions.Y * 4);
                }
            }
        }

        private void SliderTransparency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized) return;

            CanvasCropRegions.Opacity = SliderTransparency.Value;
        }

        private void TextBoxNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsInitialized) return;

            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBoxNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            TextBox t = (TextBox)sender;
            if (string.IsNullOrEmpty(t.Text))
                t.Text = "0";

            UpdateDimensions(true);
        }

        private void ButtonCrop_Click(object sender, RoutedEventArgs e)
        {
            if (imagePath == null || image == null)
            {
                MessageBox.Show("Please select an image to crop");
                return;
            }

            string fileName = PromptFilePath();

            Point dims = Dimensions;
            try
            {
                ImageCropper.CropFrames(imagePath, fileName, (int)dims.X, (int)dims.Y);
            }
            catch (ArgumentException aexc)
            {
                MessageBox.Show(aexc.Message);
                return;
            }

            if (fileName != null)
                Process.Start(System.IO.Path.GetDirectoryName(fileName));
        }

        private string PromptFilePath()
        {
            Repeat:
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Image Files|*.png";
            sfd.Title = "Select Output Location";

            bool? b = sfd.ShowDialog();
            if (!b.HasValue || !b.Value) return null;

            string dir = System.IO.Path.GetDirectoryName(sfd.FileName);
            if (Directory.GetFiles(dir).Length > 0)
            {
                MessageBoxResult mbr = MessageBox.Show("The target folder already contains files.\n\nAre you sure you want to save the frames here?", "Warning", MessageBoxButton.YesNo);
                if (mbr == MessageBoxResult.No)
                    goto Repeat;
                else if (mbr != MessageBoxResult.Yes)
                    return null;
            }

            return sfd.FileName;
        }
        private void ExtractFrames(string filePath)
        {
            if (filePath == null)
            {
                throw new NotImplementedException();
            }

            string targetFilePath = PromptFilePath();
            if (targetFilePath == null) return;
            
            ImageCropper.ExtractFrames(filePath, targetFilePath);

            Process.Start(System.IO.Path.GetDirectoryName(targetFilePath));
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            int i;
            if (int.TryParse(textBox.Text, out i))
            {
                switch (e.Key)
                {
                    default:
                        return;
                    case Key.Up:
                        i++;
                        break;
                    case Key.Down:
                        if (i > 1) i--;
                        break;
                }

                textBox.Text = i.ToString();
                UpdateDimensions(true);
                e.Handled = true;
            }
        }
    }
}
