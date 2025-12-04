using System;
using System.Drawing;

namespace YourPicker
{
    /// <summary>
    /// Utility class for color conversion functions.
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Converts HSV (Hue, Saturation, Value) color values to RGB Color.
        /// </summary>
        /// <param name="hue">Hue value (0-360 degrees)</param>
        /// <param name="saturation">Saturation value (0-1)</param>
        /// <param name="value">Value/Brightness (0-1)</param>
        /// <returns>RGB Color object</returns>
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);
            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));
            switch (hi)
            {
                case 0: return Color.FromArgb(v, t, p);
                case 1: return Color.FromArgb(q, v, p);
                case 2: return Color.FromArgb(p, v, t);
                case 3: return Color.FromArgb(p, q, v);
                case 4: return Color.FromArgb(t, p, v);
                default: return Color.FromArgb(v, p, q);
            }
        }

        /// <summary>
        /// Converts RGB Color to HSV (Hue, Saturation, Value) values.
        /// </summary>
        /// <param name="color">RGB Color to convert</param>
        /// <param name="hue">Output: Hue value (0-360 degrees)</param>
        /// <param name="saturation">Output: Saturation value (0-1)</param>
        /// <param name="value">Output: Value/Brightness (0-1)</param>
        public static void RgbToHsv(Color color, out double hue, out double saturation, out double value)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            
            if (delta == 0)
                hue = 0;
            else if (max == r)
                hue = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                hue = 60 * (((b - r) / delta) + 2);
            else
                hue = 60 * (((r - g) / delta) + 4);
            
            if (hue < 0)
                hue += 360;
            
            saturation = (max == 0) ? 0 : delta / max;
            value = max;
        }

        /// <summary>
        /// Formats a color as a HEX string.
        /// </summary>
        /// <param name="color">Color to format</param>
        /// <param name="includeAlpha">Whether to include alpha channel in output</param>
        /// <returns>HEX color string (e.g., "FF0000" or "FF0000FF")</returns>
        public static string ColorToHex(Color color, bool includeAlpha = false)
        {
            if (includeAlpha || color.A < 255)
                return $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
            else
                return $"{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Formats a color as an RGB string.
        /// </summary>
        /// <param name="color">Color to format</param>
        /// <param name="includeAlpha">Whether to include alpha channel in output</param>
        /// <returns>RGB color string (e.g., "255,0,0" or "255,0,0,255")</returns>
        public static string ColorToRgb(Color color, bool includeAlpha = false)
        {
            if (includeAlpha || color.A < 255)
                return $"{color.R},{color.G},{color.B},{color.A}";
            else
                return $"{color.R},{color.G},{color.B}";
        }
    }
}
