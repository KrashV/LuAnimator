/* Made by Guillaume "C0bra5" Mercier
 * for the currently in developpement "star cheat reloaded".
 * 
 * The Directive class is capable of taking a bitmap and a direcitve in the form of a string or
 * an instance of the Directive itself, and apply the directives to said image, all directives
 * that can be used in starbound are supported but some of the algorithms used for the scale
 * directive are pretty sloppy at best, but as for now it is the best i can do, as both imagemagik
 * and the c# graphics library deletes the color of pixels with an alpha of 0. for example,
 * if a pixel has a color value of rgba(172,23,223,0) and the graphics library executes something
 * on it, the pixel's color data will turn into rgba(0,0,0,0).
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace StarCheatReloaded.GUI
{
    public class Directive
    {
        public string Type { get; set; } = null;
        public string[] Parameters { get; set; } = null;
        


        public Directive()
        {
        }

        public Directive(string type, params string[] parameters)
        {
            Type = type;
            Parameters = parameters;
        }

        public Directive(string directive)
        {
            try
            {
                int equalIndex = directive.IndexOf("=");
                if (equalIndex == -1)
                {
                    Type = directive.Substring(0, directive.Length - 1);
                }

                if (directive.StartsWith("replace"))
                {
                    equalIndex = directive.IndexOf(";");
                }
                Type = directive.Substring(0, equalIndex);
                Parameters = directive.Substring(equalIndex + 1).Split(';');
            }
            catch
            {
                return;
            }
        }
        public override string ToString()
        {
            if (Parameters == null || Parameters.Length == 0)
            {
                return "?" + Type;
            }
            else
            {
                if (Type == "replace")
                {
                    return "?" + Type + ";" + string.Join(";", Parameters);
                }
                else
                {
                    return "?" + Type + "=" + string.Join(";", Parameters);
                }
            }
        }


        public Directive(string directive, bool skipFirstChar) : this(skipFirstChar ? directive : directive.Substring(1))
        {
            //why does this need a body? o well i don't really care.
        }

        //overrides a hoi
        public static void ApplyDirectives(ref Bitmap b, string directives)
        {
            ApplyDirectives(Graphics.FromImage(b), ref b, directives.Split('?'));
        }
        public static void ApplyDirectives(Graphics g, ref Bitmap b, string directives)
        {
            ApplyDirectives(g, ref b, directives.Split('?'));
        }
        public static void ApplyDirectives(ref Bitmap b, string[] directives)
        {
            ApplyDirectives(Graphics.FromImage(b), ref b, directives);
        }
        //the real thing
        public static void ApplyDirectives(Graphics g, ref Bitmap b, string[] directives)
        {
            foreach (string dirText in directives.Skip(1))
            {
                Directive dir = new Directive(dirText);
                switch (dir.Type)
                {
                    case "replace":
                        applyReplaceDirective(g, ref b, dir);
                        break;
                    case "setcolor":
                        applySetColorDirective(g, ref b, dir);
                        break;
                    case "flipx":
                        applyFlipXDirective(ref b);
                        break;
                    case "flipy":
                        applyFlipYDirective(ref b);
                        break;
                    case "flipxy":
                        applyFlipXYDirective(ref b);
                        break;
                    case "scale":
                        applyScaleDirective(ref g, ref b, dir);
                        break;
                    case "scalenearest":
                        applyScaleNeighboursDirective(ref g, ref b, dir);
                        break;
                    case "scalebicubic":
                        applyScaleCubicDirective(ref g, ref b, dir);
                        break;
                    case "scalebilinear":
                        applyScaleBilinearDirective(ref g, ref b, dir);
                        break;
                    case "crop":
                        applyCropDirective(ref g, ref b, dir);
                        break;
                    case "blendmult":
                        applyBlendMultDirective(ref b, dir);
                        break;
                    case "blendscreen":
                        applyBlendScreenDirective(ref b, dir);
                        break;
                    case "fade":
                        applyCropDirective(ref g, ref b, dir);
                        break;
                    case "hueshift":
                        applyHueShiftDirective(ref b, dir);
                        break;
                    case "saturation":
                        applySaturationShiftDirective(ref b, dir);
                        break;
                    case "brightness":
                        applyBirghtnessShiftDirective(ref b, dir);
                        break;
                    case "addmask":
                        applyAddMaskDirective(ref b, dir);
                        break;
                    case "submask":
                        applySubMaskDirective(ref b, dir);
                        break;
                }
            }
            g.Dispose();
        }

        private static void applyReplaceDirective(Graphics g, ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
                return;

            ColorMap[] colorMaps = new ColorMap[d.Parameters.Length];
            for (int i = 0; i < d.Parameters.Length; i++)
            {
                string[] colors = d.Parameters[i].Split('=');
                colorMaps[i] = new ColorMap()
                {
                    OldColor = RGBAtoColor(colors[0]),
                    NewColor = RGBAtoColor(colors[1])
                };
            }

            int w = b.Width;
            int h = b.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, b.PixelFormat);

            //get total bytes in image
            int bytes = Math.Abs(src_unlocked.Stride) * h;

            //get apointer that starts at the first pixel
            IntPtr pointer = src_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] bgra = new byte[bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(pointer, bgra, 0, bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int i = 0; i < bytes; i += 4)
            {
                foreach (ColorMap cm in colorMaps)
                {
                    if (cm.OldColor.B == bgra[i] && cm.OldColor.G == bgra[i + 1] && cm.OldColor.R == bgra[i + 2] && cm.OldColor.A == bgra[i + 3])
                    {
                        bgra[i] = cm.NewColor.B;
                        bgra[i + 1] = cm.NewColor.G;
                        bgra[i + 2] = cm.NewColor.R;
                        bgra[i + 3] = cm.NewColor.A;
                    }
                }
            }


            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bgra, 0, pointer, bytes);

            b.UnlockBits(src_unlocked);
            /* using the graphics library it screws up why? it blanks out the color in the transparent pixels and it screws up all sorts of things.
			ImageAttributes attrs = new ImageAttributes();
			attrs.SetRemapTable(colorMaps);
			g.CompositingMode = CompositingMode.SourceCopy;
			g.DrawImage(b,new Rectangle(0,0,b.Width,b.Height),0,0,b.Width,b.Height,GraphicsUnit.Pixel,attrs);
			*/
        }

        private static void applySetColorDirective(Graphics g, ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
                return;

            SolidBrush brush = new SolidBrush(RGBAtoColor(d.Parameters[0]));
            g.FillRectangle(brush, 0, 0, b.Width, b.Height);
            brush.Dispose();
        }
        private static void applyFlipXDirective(ref Bitmap b)
        {
            b.RotateFlip(RotateFlipType.RotateNoneFlipX);
        }
        private static void applyFlipYDirective(ref Bitmap b)
        {
            b.RotateFlip(RotateFlipType.RotateNoneFlipX);
        }
        private static void applyFlipXYDirective(ref Bitmap b)
        {
            b.RotateFlip(RotateFlipType.RotateNoneFlipX);
        }
        private static void applyScaleDirective(ref Graphics g, ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
                return;

            float x, y;
            if (d.Parameters.Length == 1)
            {
                x = y = float.Parse(d.Parameters[0]);
            }
            else
            {
                x = float.Parse(d.Parameters[0]);
                y = float.Parse(d.Parameters[1]);
            }
            int w1 = b.Width;
            int h1 = b.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w1, h1), ImageLockMode.ReadWrite, b.PixelFormat);

            //get total bytes in image
            int bytes = Math.Abs(src_unlocked.Stride) * h1;

            //get apointer that starts at the first pixel
            IntPtr pointer = src_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] bgra = new byte[bytes];
            int w2 = (int)Math.Floor(w1 * x);
            int h2 = (int)Math.Floor(h1 * y);


            byte[] temp = new byte[w2 * h2];

            // EDIT: added +1 to account for an early rounding problem
            int x_ratio = (int)((w1 << 16) / w2) + 1;
            int y_ratio = (int)((h1 << 16) / h2) + 1;

            int x2, y2;
            for (int i = 0; i < h2; i++)
            {
                for (int j = 0; j < w2; j++)
                {
                    x2 = ((j * x_ratio) >> 16);
                    y2 = ((i * y_ratio) >> 16);
                    temp[(i * w2) + j] = bgra[(y2 * w1) + x2];
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(bgra, 0, pointer, bytes);

            b.UnlockBits(src_unlocked);
        }
        private static void applyScaleNeighboursDirective(ref Graphics g, ref Bitmap b, Directive d)
        {
            //note: don't use the graphics library, it blanks out the color in transparent pixels, and we don't want that.

            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
                return;

            int xFactor, yFactor;
            if (d.Parameters.Length == 1)
            {
                xFactor = yFactor = int.Parse(d.Parameters[0]);
            }
            else
            {
                xFactor = int.Parse(d.Parameters[0]);
                yFactor = int.Parse(d.Parameters[1]);
            }

            Bitmap trg = new Bitmap(b.Width * xFactor, b.Height * yFactor, b.PixelFormat);
            using (Graphics gr = Graphics.FromImage(trg))
            {
                gr.InterpolationMode = InterpolationMode.NearestNeighbor;
                gr.DrawImage(b, 0, 0, (b.Width * xFactor), (b.Height * yFactor));
            }

            /*
            int src_w = b.Width;
            int src_h = b.Height;
            int trg_w = trg.Width;
            int trg_h = trg.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, src_w, src_h), ImageLockMode.ReadWrite, b.PixelFormat);
            BitmapData trg_unlocked = trg.LockBits(new Rectangle(0, 0, trg_w, trg_h), ImageLockMode.ReadWrite, trg.PixelFormat);

            //get total bytes in image
            int src_bytes = Math.Abs(src_unlocked.Stride) * src_h;
            int trg_bytes = Math.Abs(trg_unlocked.Stride) * src_h;

            //get apointer that starts at the first pixel
            IntPtr src_pointer = src_unlocked.Scan0;
            IntPtr trg_pointer = trg_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] src_bgra = new byte[src_bytes];
            byte[] trg_bgra = new byte[trg_bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(src_pointer, src_bgra, 0, src_bytes);
            System.Runtime.InteropServices.Marshal.Copy(trg_pointer, trg_bgra, 0, trg_bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            int y = 0, x = 0, h = 0, v = 0;

            for (y = 0; y < src_h; y += 1)
            {
                for (x = 0; x < src_w; x += 1)
                {
                    for (v = 0; v < yFactor; v++)
                    {
                        for (h = 0; h < xFactor; h++)
                        {
                            trg_bgra[(((y + v) * trg_w) + ((x * xFactor) + h)) * 4] = src_bgra[((y * src_w) + x) * 4];
                            trg_bgra[(((y + v) * trg_w) + ((x * xFactor) + h)) * 4 + 1] = src_bgra[((y * src_w) + x) * 4 + 1];
                            trg_bgra[(((y + v) * trg_w) + ((x * xFactor) + h)) * 4 + 2] = src_bgra[((y * src_w) + x) * 4 + 2];
                            trg_bgra[(((y + v) * trg_w) + ((x * xFactor) + h)) * 4 + 3] = src_bgra[((y * src_w) + x) * 4 + 3];
                        }
                    }
                }
            }

            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(src_bgra, 0, src_pointer, src_bytes);
            System.Runtime.InteropServices.Marshal.Copy(trg_bgra, 0, trg_pointer, trg_bytes);

            b.UnlockBits(src_unlocked);
            trg.UnlockBits(trg_unlocked);
            */
            b.Dispose();
            g.Dispose();
            b = trg;
            g = Graphics.FromImage(b);

        }
        private static void applyScaleCubicDirective(ref Graphics g, ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
                return;

            float x, y;
            if (d.Parameters.Length == 1)
            {
                x = y = float.Parse(d.Parameters[0]);
            }
            else
            {
                x = float.Parse(d.Parameters[0]);
                y = float.Parse(d.Parameters[1]);
            }
            //take default, replace, apply transform with new mode, re-apply default
            InterpolationMode defaultMode = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.Bicubic;
            g.ScaleTransform(x, y);
            g.InterpolationMode = defaultMode;
        }
        private static void applyScaleBilinearDirective(ref Graphics g, ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
                return;

            float x, y;
            if (d.Parameters.Length == 1)
            {
                x = y = float.Parse(d.Parameters[0]);
            }
            else
            {
                x = float.Parse(d.Parameters[0]);
                y = float.Parse(d.Parameters[1]);
            }
            //take default, replace, apply transform with new mode, re-apply default
            InterpolationMode defaultMode = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.ScaleTransform(x, y);
            g.InterpolationMode = defaultMode;
        }

        private static void applyCropDirective(ref Graphics g, ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
            {
                return;
            }

            int left, bottom, right, top;
            left = Convert.ToInt32(d.Parameters[0]);
            bottom = Convert.ToInt32(d.Parameters[1]);
            right = Convert.ToInt32(d.Parameters[2]);
            top = Convert.ToInt32(d.Parameters[3]);

            Bitmap newBitmap = b.Clone(new Rectangle(left, b.Height - top, right - left, top - bottom), b.PixelFormat);
            b.Dispose();
            g.Dispose();
            b = newBitmap;
            g = Graphics.FromImage(b);
        }

        private static void applyBlendMultDirective(ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
            {
                return;
            }

            //here we will consider using signPlaceHolder image only
            Bitmap bmp = LuAnimatorV2.Properties.Resources.signplaceholder;
            Bitmap toApply = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height),
            PixelFormat.Format32bppArgb);
            int trg_w = toApply.Width;
            int trg_h = toApply.Height;

            int xOffset = 0;
            int yOffset = 0;
            if (d.Parameters.Length >= 2)
                xOffset = int.Parse(d.Parameters[1]);
            if (d.Parameters.Length >= 3)
                yOffset = int.Parse(d.Parameters[2]);

            int src_w = b.Width;
            int src_h = b.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, src_w, src_h), ImageLockMode.ReadWrite, b.PixelFormat);
            BitmapData trg_unlocked = toApply.LockBits(new Rectangle(0, 0, trg_w, trg_h), ImageLockMode.ReadWrite, toApply.PixelFormat);

            //get total bytes in image
            int src_bytes = Math.Abs(src_unlocked.Stride) * src_h;
            int trg_bytes = Math.Abs(trg_unlocked.Stride) * trg_h;

            //get apointer that starts at the first pixel
            IntPtr src_pointer = src_unlocked.Scan0;
            IntPtr trg_pointer = trg_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] src_bgra = new byte[src_bytes];
            byte[] trg_bgra = new byte[trg_bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(src_pointer, src_bgra, 0, src_bytes);
            System.Runtime.InteropServices.Marshal.Copy(trg_pointer, trg_bgra, 0, trg_bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int y = 0; y < trg_h; y += 1)
            {
                for (int x = 0; x < trg_w; x += 1)
                {
                    int src_x = x - xOffset;
                    int src_y = ((src_h - trg_h) + y) + yOffset;
                    //just some notes because i had a brain fart when writing that function
                    //src_w = 8                          
                    //trg_w = 16                         
                    //xOffset = 4                        
                    //                                   
                    //  src_x = x - xOffset              
                    //                                   
                    //   x = 4           x = 11          
                    //  0 <= 4 - 4           11 - 4 < 8  
                    //  0 <= 0                    7 < 8  
                    //  true                       true  
                    //                                   
                    //   x = 3           x = 12          
                    //  0 <= 3 - 4           12 - 4 < 8  
                    //  0 <= -1                   8 < 8  
                    //  false                     false  
                    if (0 <= src_x && src_x < src_w)
                    {

                        //just some notes because i had a brain fart when writing that function
                        //  src_h = 4                                       
                        //  trg_h = 8                                       
                        //  yOffset = 3                                     
                        //                                                  
                        //  src_y = ((src_h - trg_h) + y) + yOffset         
                        //                                                  
                        //              y = 1               y = 4           
                        //  0 <= ((4 - 8) + 1) + 3   ((4 - 8) + 4) + 3 < 4  
                        //  0 <=    ((-4) + 1) + 3      ((-4) + 4) + 3 < 4  
                        //  0 <=            -3 + 3             (0) + 3 < 4  
                        //  0 <= 0                                   3 < 4  
                        //  true                                      true  
                        //                                                  
                        //              y = 0               y = 5           
                        //  0 <= ((4 - 8) + 0) + 3   ((4 - 8) + 5) + 3 < 4  
                        //  0 <=    ((-4) + 0) + 3      ((-4) + 5) + 3 < 4  
                        //  0 <=            -4 + 3             (1) + 3 < 4  
                        //  0 <= -1                                  4 < 4  
                        //  false                                    false  
                        //                                                  
                        //              y = 5               y = 5           
                        //  0 <= ((4 - 8) + 5) + 3   ((4 - 8) + 5) + 3 < 4  
                        //  0 <=    ((-4) + 5) + 3      ((-4) + 5) + 3 < 4  
                        //  0 <=           (1) + 3             (1) + 3 < 4  
                        //  0 <= 4                                   4 < 4  
                        //  true                                     false  
                        //                                                  
                        //              y = 4               y = 4           
                        //  0 <= ((4 - 8) + 4) + 3   ((4 - 8) + 4) + 3 < 4  
                        //  0 <=    ((-4) + 4) + 3      ((-4) + 4) + 3 < 4  
                        //  0 <=           (0) + 3             (0) + 3 < 4  
                        //  0 <= 3                                   3 < 4  
                        //  true                                     true   
                        if (0 <= src_y && src_y < src_h)
                        {



                            /* Target Blue  */
                            byte trgB = trg_bgra[((y * trg_w) + x) * 4];
                            /* Target Green */
                            byte trgG = trg_bgra[(((y * trg_w) + x) * 4) + 1];
                            /* Target Red   */
                            byte trgR = trg_bgra[(((y * trg_w) + x) * 4) + 2];
                            /* Target Alpha */
                            byte trgA = trg_bgra[(((y * trg_w) + x) * 4) + 3];

                            /* Source Blue  */
                            double srcB = src_bgra[((src_y * src_w) + src_x) * 4];
                            /* Source Green */
                            double srcG = src_bgra[(((src_y * src_w) + src_x) * 4) + 1];
                            /* Source Red   */
                            double srcR = src_bgra[(((src_y * src_w) + src_x) * 4) + 2];
                            /* Source Alpha */
                            double srcA = src_bgra[(((src_y * src_w) + src_x) * 4) + 3];
                            src_bgra[((src_y * src_w) + src_x) * 4] = Convert.ToByte(Math.Round(((srcB / 255f) * (trgB / 255f)) * 255f));
                            src_bgra[(((src_y * src_w) + src_x) * 4) + 1] = Convert.ToByte(Math.Round(((srcG / 255f) * (trgG / 255f)) * 255f));
                            src_bgra[(((src_y * src_w) + src_x) * 4) + 2] = Convert.ToByte(Math.Round(((srcR / 255f) * (trgR / 255f)) * 255f));
                            src_bgra[(((src_y * src_w) + src_x) * 4) + 3] = Convert.ToByte(Math.Round(((srcA / 255f) * (trgA / 255f)) * 255f));
                        }


                    }

                }
            }
            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(src_bgra, 0, src_pointer, src_bytes);

            b.UnlockBits(src_unlocked);
            toApply.UnlockBits(trg_unlocked);
        }

        private static void applyBlendScreenDirective(ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
            {
                return;
            }

            //here we will consider using signPlaceHolder image only
            Bitmap toApply = new Bitmap(LuAnimatorV2.Properties.Resources.signplaceholder);
            int xOffset = 8;
            int yOffset = 8;

            int src_w = b.Width;
            int src_h = b.Height;
            int trg_w = toApply.Width;
            int trg_h = toApply.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, src_w, src_h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData trg_unlocked = toApply.LockBits(new Rectangle(0, 0, src_w, src_h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //get total bytes in image
            int src_bytes = Math.Abs(src_unlocked.Stride) * src_h;
            int trg_bytes = Math.Abs(trg_unlocked.Stride) * src_h;

            //get apointer that starts at the first pixel
            IntPtr src_pointer = src_unlocked.Scan0;
            IntPtr trg_pointer = trg_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] src_bgra = new byte[src_bytes];
            byte[] trg_bgra = new byte[trg_bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(src_pointer, src_bgra, 0, src_bytes);
            System.Runtime.InteropServices.Marshal.Copy(trg_pointer, trg_bgra, 0, trg_bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int y = 0; y < trg_h; y += 1)
            {
                for (int x = 0; x < trg_w; x += 1)
                {
                    int src_x = x - xOffset;
                    if (0 <= src_x && src_x > src_w)
                    {

                        int src_y = ((src_h - trg_h) + y) + yOffset;
                        if (0 <= src_y && src_y < src_h)
                        {

                            /* Target Blue  */
                            float trgB = trg_bgra[((y * trg_w) + x) * 4] / 255f;
                            /* Target Green */
                            float trgG = trg_bgra[(((y * trg_w) + x) * 4) + 1] / 255f;
                            /* Target Red   */
                            float trgR = trg_bgra[(((y * trg_w) + x) * 4) + 2] / 255f;
                            /* Target Alpha */
                            float trgA = trg_bgra[(((y * trg_w) + x) * 4) + 3] / 255f;

                            /* Source Blue  */
                            float srcB = src_bgra[((src_y * src_w) + src_x) * 4] / 255f;
                            /* Source Green */
                            float srcG = src_bgra[(((src_y * src_w) + src_x) * 4) + 1] / 255f;
                            /* Source Red   */
                            float srcR = src_bgra[(((src_y * src_w) + src_x) * 4) + 2] / 255f;
                            /* Source Alpha */
                            float srcA = src_bgra[(((src_y * src_w) + src_x) * 4) + 3] / 255f;

                            src_bgra[((src_y * src_w) + src_x) * 4] = Convert.ToByte(Math.Floor((1 - ((1 - srcB) * (1 - trgB))) * 255));
                            src_bgra[(((src_y * src_w) + src_x) * 4) + 1] = Convert.ToByte(Math.Floor((1 - ((1 - srcG) * (1 - trgG))) * 255));
                            src_bgra[(((src_y * src_w) + src_x) * 4) + 2] = Convert.ToByte(Math.Floor((1 - ((1 - srcR) * (1 - trgR))) * 255));
                            src_bgra[(((src_y * src_w) + src_x) * 4) + 3] = Convert.ToByte(Math.Floor((1 - ((1 - srcA) * (1 - trgA))) * 255));
                        }


                    }

                }
            }
            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(src_bgra, 0, src_pointer, src_bytes);

            b.UnlockBits(src_unlocked);
            toApply.UnlockBits(trg_unlocked);
        }

        private static void applyFadeDirective(ref Graphics g, ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
            {
                return;
            }

            float fadeAmount = 1 - float.Parse(d.Parameters[0]);

            int w = b.Width;
            int h = b.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, b.PixelFormat);

            //get total bytes in image
            int bytes = Math.Abs(src_unlocked.Stride) * h;

            //get apointer that starts at the first pixel
            IntPtr pointer = src_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] bgra = new byte[bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(pointer, bgra, 0, bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int i = 0; i < bytes; i += 4)
            {
                bgra[i + 3] = Convert.ToByte((Math.Floor(bgra[i + 3] * fadeAmount)));
            }

            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bgra, 0, pointer, bytes);

            b.UnlockBits(src_unlocked);
        }

        private static void applyHueShiftDirective(ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
            {
                return;
            }
            float shift = float.Parse(d.Parameters[1]);
            int w = b.Width;
            int h = b.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //get total bytes in image
            int bytes = Math.Abs(src_unlocked.Stride) * h;

            //get apointer that starts at the first pixel
            IntPtr pointer = src_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] bgra = new byte[bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(pointer, bgra, 0, bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int i = 0; i < bytes; i += 4)
            {
                //because logic
                byte[] argb = null;//ImageUtilities.hueShiftColorBytes(bgra[i + 2], bgra[i + 1], bgra[i], bgra[i + 3], shift);
                bgra[i + 3] = argb[0];
                bgra[i + 2] = argb[1];
                bgra[i + 1] = argb[2];
                bgra[i] = argb[3];
            }

            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bgra, 0, pointer, bytes);

            b.UnlockBits(src_unlocked);
        }

        private static void applySaturationShiftDirective(ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
            {
                return;
            }
            float shift = float.Parse(d.Parameters[1]);
            int w = b.Width;
            int h = b.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //get total bytes in image
            int bytes = Math.Abs(src_unlocked.Stride) * h;

            //get apointer that starts at the first pixel
            IntPtr pointer = src_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] bgra = new byte[bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(pointer, bgra, 0, bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int i = 0; i < bytes; i += 4)
            {
                //because logic
                byte[] argb = null;// ImageUtilities.saturationShiftColorBytes(bgra[i + 2], bgra[i + 1], bgra[i], bgra[i + 3], shift);
                bgra[i + 3] = argb[0];
                bgra[i + 2] = argb[1];
                bgra[i + 1] = argb[2];
                bgra[i] = argb[3];
            }

            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bgra, 0, pointer, bytes);

            b.UnlockBits(src_unlocked);
        }

        private static void applyBirghtnessShiftDirective(ref Bitmap b, Directive d)
        {
            //null check to kick out un-needed directives
            if (d.Parameters.Length == 0)
            {
                return;
            }
            float shift = float.Parse(d.Parameters[1]);
            int w = b.Width;
            int h = b.Height;

            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //get total bytes in image
            int bytes = Math.Abs(src_unlocked.Stride) * h;

            //get apointer that starts at the first pixel
            IntPtr pointer = src_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] bgra = new byte[bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(pointer, bgra, 0, bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int i = 0; i < bytes; i += 4)
            {
                //because logic
                byte[] argb = null;//ImageUtilities.BrightnessShiftColorBytes(bgra[i + 2], bgra[i + 1], bgra[i], bgra[i + 3], shift);
                bgra[i + 3] = argb[0];
                bgra[i + 2] = argb[1];
                bgra[i + 1] = argb[2];
                bgra[i] = argb[3];
            }

            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bgra, 0, pointer, bytes);

            b.UnlockBits(src_unlocked);
        }

        private static void applyAddMaskDirective(ref Bitmap b, Directive d)
        {

            //here we will consider using signPlaceHolder image only
            Bitmap mask = new Bitmap(LuAnimatorV2.Properties.Resources.signplaceholder);
            int w = b.Width;
            int h = b.Height;
            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData mask_unlocked = mask.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //get total bytes in image
            int src_bytes = Math.Abs(src_unlocked.Stride) * h;
            int mask_bytes = Math.Abs(mask_unlocked.Stride) * h;

            //get apointer that starts at the first pixel
            IntPtr src_pointer = src_unlocked.Scan0;
            IntPtr mask_pointer = mask_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] src_rgb = new byte[src_bytes];
            byte[] mask_rgb = new byte[mask_bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(src_pointer, src_rgb, 0, src_bytes);
            System.Runtime.InteropServices.Marshal.Copy(mask_pointer, mask_rgb, 0, mask_bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int i = 0; i < src_bytes; i += 4)
            {
                if (src_rgb[i + 3] > mask_rgb[i + 3])
                    src_rgb[i + 3] = mask_rgb[i + 3];
            }
            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(src_rgb, 0, src_pointer, src_bytes);

            b.UnlockBits(src_unlocked);
            mask.UnlockBits(mask_unlocked);
            mask.Dispose();
        }

        private static void applySubMaskDirective(ref Bitmap b, Directive d)
        {

            //here we will consider using signPlaceHolder image only
            Bitmap mask = new Bitmap(LuAnimatorV2.Properties.Resources.signplaceholder);
            int w = b.Width;
            int h = b.Height;
            BitmapData src_unlocked = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData mask_unlocked = mask.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //get total bytes in image
            int src_bytes = Math.Abs(src_unlocked.Stride) * h;
            int mask_bytes = Math.Abs(mask_unlocked.Stride) * h;

            //get apointer that starts at the first pixel
            IntPtr src_pointer = src_unlocked.Scan0;
            IntPtr mask_pointer = mask_unlocked.Scan0;

            //create a byte array for quick editing or rgba values
            byte[] src_rgb = new byte[src_bytes];
            byte[] mask_rgb = new byte[mask_bytes];

            //copy entire content of pointer to byte array.
            System.Runtime.InteropServices.Marshal.Copy(src_pointer, src_rgb, 0, src_bytes);
            System.Runtime.InteropServices.Marshal.Copy(mask_pointer, mask_rgb, 0, mask_bytes);

            //0 = blue
            //1 = green
            //2 = red
            //3 = alpha
            //edit content
            for (int i = 0; i < src_bytes; i += 4)
            {
                if (src_rgb[i + 3] > mask_rgb[i + 3])
                    src_rgb[i + 3] = mask_rgb[i + 3];
            }

            //Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(src_rgb, 0, src_pointer, src_bytes);

            b.UnlockBits(src_unlocked);
            mask.UnlockBits(mask_unlocked);
            mask.Dispose();
        }


        private static Color RGBAtoColor(string hex)
        {
            hex = "#" + hex;
            Color color = new Color();
            switch (hex.Length)
            {
                case 7:
                    {
                        color = ColorTranslator.FromHtml(hex);
                        break;
                    }
                case 9:
                    {
                        string cuthex = hex.Substring(0, 7);
                        string alpha = hex.Substring(7, 2);

                        int opacity = Convert.ToInt32(alpha, 16);

                        color = Color.FromArgb(opacity, ColorTranslator.FromHtml(cuthex));
                        break;
                    }
                default:
                    throw new FormatException("Unsupported color format");
            }

            return color;
        }
    }
}
