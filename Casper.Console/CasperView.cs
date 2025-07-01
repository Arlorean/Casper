using System;
using System.Collections.Generic;
using System.Diagnostics;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Casper.Console {
    public class CasperView : Renderable {
        readonly Spectrum spectrum;

        public CasperView() {
            spectrum = new Spectrum();
            spectrum.Screen.RenderPixel += RenderPixel;

            spectrum.LoadROM(Resources.Get(Shared.Resources.Assembly, @"Resources\Roms\48.rom"));
            spectrum.Load(new FileFormats.Tap(Resources.Get(Shared.Resources.Assembly, @"Resources\Games\ManicMiner.tap")));
        }

        void RenderPixel(int x, int y, ColorIndex16 color) {
            // Full render each frame for now
        }

        bool GetPixel(int x, int y) {
            var xChar = x / 8;
            var pixels = spectrum.Screen.GetByte(xChar, y);
            var bitX = 7 - (x % 8); // Bits order on screen: [76543210]
            var mask = 1 << bitX;
            return (pixels & mask) != 0;
        }

        Octant GetOctant(int x, int y) {
            var octant = new Octant();
            for (var dy = 0; dy < Octant.PixelsHigh; dy++) {
                for (var dx = 0; dx < Octant.PixelsWide; dx++) {
                    octant[dx, dy] = GetPixel(x + dx, y + dy);
                }
            }
            // octant[0, 0] = GetPixel(x + 0, y + 0);
            // octant[0, 1] = GetPixel(x + 1, y + 0);
            // octant[1, 0] = GetPixel(x + 0, y + 1);
            // octant[1, 1] = GetPixel(x + 1, y + 1);
            // octant[2, 0] = GetPixel(x + 0, y + 2);
            // octant[2, 1] = GetPixel(x + 1, y + 2);
            // octant[3, 0] = GetPixel(x + 0, y + 3);
            // octant[3, 1] = GetPixel(x + 1, y + 3);
            return octant;
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) {
            var segments = new List<Segment>();

            for (var y = 0; y < Screen.PixelsHigh; y += Octant.PixelsHigh) {
                if (y > 0) {
                    segments.Add(Segment.LineBreak);
                }
                for (var x = 0; x < Screen.PixelsWide; x += Octant.PixelsWide) {
                    var octant = GetOctant(x, y);
                    var rune = octant.ToRune();
                    var attr = spectrum.Screen.GetAttr(x/8, y/8);
                    var style = Colors.GetStyle(attr);
                    segments.Add(new Segment(rune.ToString(), style));
                }
            }

            return segments;
        }
        
        [STAThread]
        static void Main() {
            var casper = new CasperView();

            var console = AnsiConsole.Console;
            console.AlternateScreen(() => {
                // Resize terminal to 128x48 and hide cursor
                console.Write("\x1b[8;48;128t");
                console.Cursor.Hide();

                var timer = Stopwatch.StartNew();
                while (true) {
                    timer.Restart();

                    casper.spectrum.Step();

                    console.Cursor.SetPosition(0, 0);
                    console.Write(casper);

                    var elapsed = timer.Elapsed;
                    if (elapsed < TimeSpan.FromMilliseconds(20)) {
                        System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(20) - elapsed);
                    }
                    
                    if (console.Input.IsKeyAvailable()) {
                        var key = console.Input.ReadKey(true)!;
                        if (key.Value.Key == ConsoleKey.Escape) {
                            break; // exit the loop on Escape
                        }
                    }
                }
                
                // Reset terminal size and show cursor
                console.Cursor.Show();
                console.Write("\x1b[8;24;80t");
            });
        }
    }
}
