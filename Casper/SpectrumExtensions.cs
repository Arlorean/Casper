using Casper.FileFormats;

namespace Casper {
    public static class SpectrumExtensions {
        public static void Load(this Spectrum spectrum, ISnapshot snapshot) {
            snapshot.Load(spectrum);
        }
    }
}