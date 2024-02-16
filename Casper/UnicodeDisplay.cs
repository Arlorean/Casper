using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casper {
    public static class UnicodeDisplay {
        const char HalfWidthStart = '\u0020'; // Halfwidth Forms ASCII
        const char FullWidthStart = '\uFF00'; // Fullwidth Forms ASCII
        const char BaseStart = '\uE000'; // Private Use Area (PUA) (16 4x1 pixel bases)
        const char MarkStart = '\u0300'; // Combining Diacritical Marks (16 4x1 pixel marks)

        static (char left,char right) GetNibbles(byte pixels, bool isBase) {
            var start = isBase ? BaseStart : MarkStart;
            return ((char)(start + (pixels >> 4)), (char)(start + (pixels & 15)));
        }

        public static string ToUnicode(Screen screen) {
            var sb = new StringBuilder();

            for (var yChar = 0; yChar < 24; yChar++) {
                for (var xChar = 0; xChar < 32; xChar++) {
                    // TODO: Colors
                    var colors = screen.GetColors(xChar, yChar);

                    var left = new StringBuilder();
                    var right = new StringBuilder();

                    for (var y=7; y >= 0; y--) { 
                        var yPixel = (yChar * 8) + y;
                        var pixels = screen.GetPixels(xChar, yPixel);
                        var nibbles = GetNibbles(pixels, isBase: y == 7);

                        left.Append(nibbles.left);
                        right.Append(nibbles.right);
                    }

                    sb.Append(left.ToString());
                    sb.Append(right.ToString());
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
