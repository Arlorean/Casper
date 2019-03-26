using System.Drawing;

namespace Casper {
    public interface IDisplay {
        void RenderPixel(int x, int y, bool pixel, byte attr);
    }
}
