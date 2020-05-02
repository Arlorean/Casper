using System;
using System.Collections.Generic;
using System.Text;

namespace Casper {
    internal static class BitTestExtensions {
        internal static bool IsBit0Set(this byte b) {
            return (b & 1) != 0;
        }

        internal static bool IsBit7Set(this byte b) {
            return (b & 0b1000_0000) != 0;
        }
    }
}
