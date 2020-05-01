using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Casper {
    public enum ColorIndex : byte {
        Black,
        Blue,
        Red,
        Magenta,
        Green,
        Cyan,
        Yellow,
        White,

        Bright = 8,
    };

    public static class Colors {
        // https://en.wikipedia.org/wiki/ZX_Spectrum_graphic_modes#Colour_palette
        public static readonly Color[] Palette = {
            // Bright = 0
            Color.FromArgb(0,0,0),
            Color.FromArgb(0,0,N),
            Color.FromArgb(N,0,0),
            Color.FromArgb(N,0,N),
            Color.FromArgb(0,N,0),
            Color.FromArgb(0,N,N),
            Color.FromArgb(N,N,0),
            Color.FromArgb(N,N,N),
            // Bright = 1
            Color.Black,                 
            Color.Blue,
            Color.Red,                   
            Color.Magenta,
            Color.Green,                 
            Color.Cyan,
            Color.Yellow,                
            Color.White,
        };
        const int N = 215;

        internal static ColorIndex ForegroundIndex(int attr) {
            return (ColorIndex)(((attr & 0b01000000) >> 3) | (attr & 0b00000111));
        }

        internal static ColorIndex BackgroundIndex(int attr) {
            return (ColorIndex)((attr & 0b01111000) >> 3);
        }
    }
}
