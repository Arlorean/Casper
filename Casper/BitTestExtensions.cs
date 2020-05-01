using System;
using System.Collections.Generic;
using System.Text;

namespace Casper {
    internal static class BitTestExtensions {
        internal static bool IsBit0Set(this byte b) {
            return (b & 1) == 1;
        }
    }
}
