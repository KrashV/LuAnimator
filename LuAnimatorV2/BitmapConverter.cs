using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace LuAnimatorV2
{
    /// <summary>
    /// Converter between <see cref="Bitmap"/> and <see cref="BitmapSource"/>/>
    /// </summary>
    static class BitmapConverter
    {
        /// <summary>
        /// Convert <see cref="BitmapSource"/> into <see cref="Bitmap"/>
        /// </summary>
        /// <param name="bitmapsource">BitmapSource object to convert</param>
        /// <returns>Converted Bitmap</returns>
        public static Bitmap BitmapSourceToBitmap(BitmapSource bitmapsource)
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

        /// <summary>
        /// Convert <see cref="Bitmap"/> into <see cref="BitmapSource"/>
        /// </summary>
        /// <param name="source">Bitma object to convert</param>
        /// <returns>BitmapSouce of the given Bitmap</returns>
        public static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            System.IntPtr ip = bitmap.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   System.IntPtr.Zero, System.Windows.Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }
            bs.Freeze();
            return bs;
        }
    }
}