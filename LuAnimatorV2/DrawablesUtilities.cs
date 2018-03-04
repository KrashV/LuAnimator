using System;
using System.Text;
using Silverfeelin.StarboundDrawables;
using System.Drawing;

namespace DrawablesGeneratorTool
{
    public static class DrawableUtilities
    {
        /// <summary>
        /// Returns whether the given string is a valid positive or negative intregal number.
        /// </summary>
        /// <param name="value">The string to check for validity</param>
        /// <returns>True if the given string is a valid positive or negative integer.</returns>
        public static bool IsNumber(string value)
        {
            double test;
            double.TryParse(value, out test);

            return double.TryParse(value, out test) && test >= 0;
        }

        /// <summary>
        /// Returns whether the given path is a valid image
        /// </summary>
        /// <param name="path">The path to check for validity</param>
        /// <returns>True if the given string is an image.</returns>
        public static bool IsValidImage(string path)
        {
            return Convert.ToBoolean(path.IndexOf(".png") + path.IndexOf(".jpg") + path.IndexOf(".jpeg") + 3); // Tricky way to convert -3 (if not an image) to zero.
        }

        /// <summary>
        /// Returns the value within the minimum and maximum bounds.
        /// If the value is smaller than the minimum value, or bigger than the maximum value, return that value instead.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="minimum">The lowest acceptable value</param>
        /// <param name="maximum">The highest acceptable value</param>
        /// <returns>The value if it's between minimum and maximum, minimum if smaller or maximum if larger.</returns>
        public static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }
            else if (value > minimum)
            {
                return maximum;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Transforms the directives from a <see cref="DrawablesOutput"/> into a single directives string.
        /// Requires the given output to be generated with ReplaceBlank and ReplaceWhite set to true.
        /// </summary>
        /// <param name="output">Output to read directives from.</param>
        /// <param name="baseScale">Base scale. Should be the biggest dimension in the source image this string will be applied to by the user.
        /// EG. If the user wishes to apply a drawable bigger than 30x50 to a 30x50 object, the base scale should be 50.</param>
        /// <returns>Directives string</returns>
        public static string GenerateSingleTextureDirectives(DrawablesOutput output, int baseScale = 64)
        {
            int w = output.ImageWidth,
                h = output.ImageHeight;

            int max = w > h ? w : h;
            int scale = (int)Math.Ceiling((double)max / baseScale);

            StringBuilder dir = new StringBuilder();
            dir.AppendFormat("?replace;00000000=ffffff;ffffff00=ffffff?setcolor=ffffff?scalenearest={0}?crop=0;0;{1};{2}", scale, w, h);

            foreach (Drawable drawable in output.Drawables)
            {
                if (drawable != null)
                    dir.AppendFormat("?blendmult={0};{1};{2}{3}", drawable.Texture, -drawable.X, -drawable.Y, drawable.Directives);
            }

            dir.Append("?replace;ffffffff=00000000");

            return dir.ToString();
        }

        /// <summary>
        /// Sets up the properties of an instantiated generator, using the given parameters.
        /// </summary>
        /// <param name="generator">Generator to set up.</param>
        /// <param name="handX">String value for the horizontal hand offset in pixels, presumably from a text field.
        /// Value should be convertable to an integer.</param>
        /// <param name="handY">String value for the vertical hand offset in pixels, presumably from a text field.
        /// Value should be convertable to an integer.</param>
        /// <param name="ignoreColor">String value of the color to ignore, presumably from a text field.
        /// If given, value should be formatted RRGGBB or RRGGBBAA (hexadecimal string).</param>
        /// <returns>Reference to the given object.</returns>
        public static DrawablesGenerator SetUpGenerator(DrawablesGenerator generator, int handX, int handY)
        {
            generator.OffsetX = Convert.ToInt32(handX) + 1;
            generator.OffsetY = Convert.ToInt32(handY);

            generator.RotateFlipStyle = System.Drawing.RotateFlipType.RotateNoneFlipY;

            generator.ReplaceBlank = true;
            generator.ReplaceWhite = true;

            return generator;
        }

        public static string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            int pos = Source.IndexOf(Find);
            if (pos < 0)
            {
                return Source;
            }
            return Source.Substring(0, pos) + Replace + Source.Substring(pos + Find.Length);
        }

        public static string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }

        public static Color ColorFromHex(string hex)
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
