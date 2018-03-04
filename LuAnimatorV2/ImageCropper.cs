using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace LuAnimatorV2
{
    public static class ImageCropper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceImagePath"></param>
        /// <param name="resultFilePath"></param>
        /// <param name="frameWidth"></param>
        /// <param name="frameHeight"></param>
        /// <exception cref="ArgumentException">Thrown when image width or height is not larger than 0.</exception>
        public static void CropFrames(string sourceImagePath, string resultFilePath, int frameWidth, int frameHeight)
        {
            if (frameWidth <= 0 || frameHeight <= 0)
                throw new ArgumentException("Frame width and height must be larger than zero.");

            string dir = Path.GetDirectoryName(resultFilePath);
            string fileName = Path.GetFileNameWithoutExtension(resultFilePath);

            if (fileName == null)
                return;

            if (fileName.IndexOf("{0}") == -1)
                fileName += "{0}";

            Image image = Image.FromFile(sourceImagePath);
            if (frameWidth > image.Width)
                frameWidth = image.Width;
            if (frameHeight > image.Height)
                frameHeight = image.Height;

            int frameCount = (int)(Math.Ceiling((double)image.Width / frameWidth) * Math.Ceiling((double)image.Height / frameHeight));
            int digits = frameCount.ToString().Length;

            int x = 0, y = 0;
            int frameNumber = 1;

            Bitmap bmp = new Bitmap(image);

            while (y < image.Height)
            {
                while (x < image.Width)
                {
                    Rectangle r = new Rectangle(x, y, frameWidth, frameHeight);
                    if (r.Width > image.Width - x)
                        r.Width = image.Width - x;
                    if (r.Height > image.Height - y)
                        r.Height = image.Height - y;

                    Bitmap croppedFrame = bmp.Clone(r, bmp.PixelFormat);

                    if (croppedFrame.Width < frameWidth || croppedFrame.Height < frameHeight)
                    {
                        Bitmap newFrame = new Bitmap(frameWidth, frameHeight);

                        using (Graphics g = Graphics.FromImage(newFrame))
                        {
                            g.DrawImage(croppedFrame, new Point(0, 0));
                        }

                        croppedFrame = newFrame;
                    }
                    
                    string sNumber = frameNumber.ToString();
                    while (sNumber.Length < digits)
                        sNumber = "0" + sNumber;

                    croppedFrame.Save(dir + "\\" + string.Format(fileName, sNumber) + ".png", ImageFormat.Png);

                    frameNumber++;

                    x += frameWidth;
                }
                x = 0;
                y += frameHeight;
            }
        }

        public static void ExtractFrames(string sourceGifPath, string resultFilePath)
        {
            // Get frames from GIF
            Image gif = Image.FromFile(sourceGifPath);
            FrameDimension dimension = new FrameDimension(gif.FrameDimensionsList[0]);

            int frameCount = gif.GetFrameCount(dimension);
            int digits = frameCount.ToString().Length;

            Image[] frames = new Image[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                gif.SelectActiveFrame(dimension, i);
                frames[i] = ((Image)gif.Clone());
            }

            string outputFolder = Path.GetDirectoryName(resultFilePath);
            string fileName = Path.GetFileNameWithoutExtension(resultFilePath);
            if (fileName.IndexOf("{0}") == -1)
                fileName += "{0}";

            int j = 1;
            foreach (Image frame in frames)
            {
                string sNumber = j.ToString();
                while (sNumber.Length < digits)
                    sNumber = "0" + sNumber;

                frame.Save(outputFolder + "\\" + string.Format(fileName, sNumber) + ".png", ImageFormat.Png);
                j++;
            }
        }
    }
}
