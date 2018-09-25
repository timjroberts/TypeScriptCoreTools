using System.IO;
using System.Reflection;

namespace ManagedNodeProcess.Utils
{
    /// <summary>
    /// Utilities for reading embedded JavaScript resources.
    /// </summary>
    public static class EmbeddedScriptResourceReader
    {
        /// <summary>
        /// Reads a JavaScript resource from the calling assembly.
        /// </summary>
        /// <param name="path">The path of the JavaScript resource to be read.</param>
        /// <returns>The contents of the resource.</returns>
        public static string ReadScript(string path)
        {
            var asm = Assembly.GetCallingAssembly();
            var embeddedResourceName = asm.GetName().Name + path.Replace("/", ".");

            using (var stream = asm.GetManifestResourceStream(embeddedResourceName))
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
