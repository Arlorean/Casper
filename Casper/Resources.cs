using System;
using System.IO;
using System.Reflection;

namespace Casper {
    public static class Resources {
        public static byte[] Get(Assembly assembly, String filename) {
            filename = Path.Combine(assembly.GetName().Name, filename);
            filename = filename.Replace('\\', '.'); // Blazor Server
            filename = filename.Replace('/', '.'); // Blazor WebAssembly
            using (Stream resFilestream = assembly.GetManifestResourceStream(filename)) {
                if (resFilestream == null) return null;
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                return ba;
            }
        }
    }
}
