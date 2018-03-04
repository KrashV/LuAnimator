using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace LuAnimatorV2
{
    static class BitmapConverter
    {
        public static Bitmap ToWinFormsBitmap(this BitmapSource bitmapsource)
        {
            Bitmap bmp = new Bitmap(
              bitmapsource.PixelWidth,
              bitmapsource.PixelHeight,
              PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              PixelFormat.Format32bppPArgb);
            bitmapsource.CopyPixels(
              System.Windows.Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        [System.Runtime.InteropServices.DllImport("gdi32")]
        static extern int DeleteObject(System.IntPtr o);

        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            System.IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   System.IntPtr.Zero, System.Windows.Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }
    }
}