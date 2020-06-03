using System;
using System.Collections.Generic;
using System.Text;

namespace Casper {
    /// <summary>
    /// Addresses of routines in the ZX Spectrum ROM (16K/48K).
    /// https://skoolkit.ca/disassemblies/rom/index.html
    /// </summary>
    public static class ROM {
        public const ushort LD_BYTES = 0x0556;
        public const ushort RAM_DONE = 0x11EF;
    }
}
