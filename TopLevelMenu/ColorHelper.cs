using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media;
using Color = System.Drawing.Color;
using Brush = System.Windows.Media.Brush;

namespace TopLevelMenu
{
    public static class ColorHelper
    {
        // Convertir une couleur de GDI+ en une couleur de WPF
        public static System.Windows.Media.Color ToWpfColor(this Color gdiColor)
        {
            return System.Windows.Media.Color.FromArgb(gdiColor.A, gdiColor.R, gdiColor.G, gdiColor.B);
        }

        // Convertir une couleur de WPF en une couleur de GDI+
        public static Color ToGdiColor(this System.Windows.Media.Color wpfColor)
        {
            return Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }

        // Convertir une couleur de GDI+ en un pinceau solide de WPF
        public static Brush ToWpfBrush(this Color gdiColor)
        {
            return new SolidColorBrush(gdiColor.ToWpfColor());
        }

        // Convertir un pinceau solide de WPF en une couleur de GDI+
        public static Color ToGdiColor(this Brush wpfBrush)
        {
            if (wpfBrush is SolidColorBrush solidBrush)
            {
                return solidBrush.Color.ToGdiColor();
            }
            else
            {
                return new Color();
            }
        }
    }

}
