using System;
using System.Collections.Generic;
using System.Text;

namespace Casper {
    /// <summary>
    /// Convert byte updates in memory to pixel updates on the screen.
    /// http://www.zxdesign.info/memoryToScreen.shtml
    /// </summary>
    public class Screen {
        byte[,] pixels = new byte[32, 192];
        byte[,] colors = new byte[32, 24];

        public event Action<int, int, ColorIndex> RenderPixel;

        internal void UpdateByte(int address, byte value) {
            if (RenderPixel == null) { return; }

            if (address < (16384+(32*192))) {
                UpdatePixels(address, value);
            }
            else {
                UpdateColors(address, value);
            }
        }

        void UpdatePixels(int address, byte newValue) {
            // http://www.zxdesign.info/memoryToScreen.shtml
            var cx = (address & 0b000_00_000_000_11111);
            var py = ((address & 0b000_11_000_000_00000) >> 5)
                   | ((address & 0b000_00_000_111_00000) >> 2)
                   | ((address & 0b000_00_111_000_00000) >> 8);
            var attr = colors[cx, py >> 3];

            var oldValue = pixels[cx, py];
            pixels[cx, py] = newValue;

            var px = cx << 3;
            var changes = (byte) (newValue ^ oldValue);
            for (var dx = 7; dx >= 0; --dx) {
                //if (changes.IsBit0Set()) {
                    var index = newValue.IsBit0Set() ? Colors.ForegroundIndex(attr) : Colors.BackgroundIndex(attr);
                    RenderPixel(px+dx, py, index);
                //}
                changes >>= 1;
                newValue >>= 1;
            }
        }

        void UpdateColors(int address, byte newValue) {
            var cx = (address & 0b000000_00000_11111);
            var cy = (address & 0b000000_11111_00000) >> 5;

            var oldValue = colors[cx, cy];
            colors[cx, cy] = newValue;

            var oldForeground = Colors.ForegroundIndex(oldValue);
            var newForeground = Colors.ForegroundIndex(newValue);
            var oldBackground = Colors.BackgroundIndex(oldValue);
            var newBackground = Colors.BackgroundIndex(newValue);

            var px = cx << 3;
            var py = cy << 3;
            for (var dy = 0; dy < 8; dy++) {
                var value = pixels[cx, py+dy];
                for (var dx = 7; dx >= 0; --dx) {
                    var isForeground = value.IsBit0Set();
                    value >>= 1;

                    if (isForeground) {
                        if (oldForeground != newForeground) {
                            RenderPixel(px+dx, py+dy, newForeground);
                        }
                    }
                    else {
                        if (oldBackground != newBackground) {
                            RenderPixel(px+dx, py+dy, newBackground);
                        }
                    }
                }
            }
        }
    }
}
