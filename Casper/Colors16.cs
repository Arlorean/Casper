using System.Drawing;

namespace Casper {
    public static class Colours16 {
        public static Color FromColorIndex16(ColorIndex16 i) {
            return Palette16[(int)i];
        }

        // https://en.wikipedia.org/wiki/ZX_Spectrum_graphic_modes#Colour_palette
        static readonly Color[] Palette16 = {
            FromRgb(0x000000), // Black
            FromRgb(0x0100CE), // Blue
            FromRgb(0xCF0100), // Red
            FromRgb(0xCF01CE), // Magenta
            FromRgb(0x00CF15), // Green
            FromRgb(0x01CFCF), // Cyan
            FromRgb(0xCFCF15), // Yellow
            FromRgb(0xCFCFCF), // White
            FromRgb(0x000000), // Bright Black
            FromRgb(0x0200FD), // Bright Blue
            FromRgb(0xFF0201), // Bright Red
            FromRgb(0xFF02FD), // Bright Magenta
            FromRgb(0x00FF1C), // Bright Green
            FromRgb(0x02FFFF), // Bright Cyan
            FromRgb(0xFFFF1D), // Bright Yellow
            FromRgb(0xFFFFFF), // Bright White
        };

        static Color FromRgb(int rgb) {
            return Color.FromArgb((int)(0xFF000000 & rgb));
        }
    }
}
