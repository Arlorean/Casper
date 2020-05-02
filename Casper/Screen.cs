using System;
using System.Collections.Generic;
using System.Text;

namespace Casper {
    /// <summary>
    /// Convert byte updates in memory to pixel updates on the screen.
    /// http://www.zxdesign.info/memoryToScreen.shtml
    /// </summary>
    public class Screen {
        readonly byte[,] pixels = new byte[32, 192];
        readonly byte[,] colors = new byte[32, 24];
        bool flashInverted;

        public ColorIndex Border { get; set; }

        public event Action<int, int, ColorIndex> RenderPixel;

        public void Flash() {
            flashInverted = !flashInverted;

            if (RenderPixel == null) { return; }

            for (var cy = 0; cy < 24; cy++) {
                for (var cx = 0; cx < 32; ++cx) {
                    var attr = colors[cx, cy];
                    if (attr.IsBit7Set()) {
                        RenderAttr(cx, cy, attr);
                    }
                }
            }
        }

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
            var cy = py >> 3;

            var oldValue = pixels[cx, py];
            pixels[cx, py] = newValue;

            var attr = colors[cx, cy];
            var foreground = Colors.ForegroundIndex(attr);
            var background = Colors.BackgroundIndex(attr);
            if (attr.IsBit7Set() && flashInverted) {
                (foreground, background) = (background, foreground);
            }

            var px = cx << 3;
            var changes = (byte) (newValue ^ oldValue);
            for (var dx = 7; dx >= 0; --dx) {
                if (changes.IsBit0Set()) {
                    var isForeground = newValue.IsBit0Set();
                    var index = isForeground ? foreground : background;
                    RenderPixel(px+dx, py, index);
                }
                changes >>= 1;
                newValue >>= 1;
            }
        }

        void UpdateColors(int address, byte attr) {
            var cx = (address & 0b000000_00000_11111);
            var cy = (address & 0b000000_11111_00000) >> 5;

            colors[cx, cy] = attr;

            RenderAttr(cx, cy, attr);
        }

        void RenderAttr(int cx, int cy, byte attr) {
            var foreground = Colors.ForegroundIndex(attr);
            var background = Colors.BackgroundIndex(attr);
            if (attr.IsBit7Set() && flashInverted) {
                (foreground, background) = (background, foreground);
            }

            var px = cx << 3;
            var py = cy << 3;
            for (var dy = 0; dy < 8; dy++) {
                var value = pixels[cx, py + dy];
                for (var dx = 7; dx >= 0; --dx) {
                    var isForeground = value.IsBit0Set();
                    RenderPixel(px + dx, py + dy, isForeground ? foreground : background);
                    value >>= 1;
                }
            }
        }
    }
}
