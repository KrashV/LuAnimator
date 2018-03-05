using System;
using System.Text;
using Silverfeelin.StarboundDrawables;

namespace DrawablesGeneratorTool
{
    public static class DrawableUtilities
    {
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
    }
}
