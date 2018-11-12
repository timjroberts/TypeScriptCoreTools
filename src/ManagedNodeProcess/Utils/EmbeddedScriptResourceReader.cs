using System.IO;
using System.Reflection;

namespace ManagedNodeProcess.Utils
{
    /// <summary>
    /// Provides utilities for reading embedded JavaScript resources.
    /// </summary>
    public static class EmbeddedScriptResourceReader
    {
        /// <summary>
        /// Reads a JavaScript resource from the calling assembly.
        /// </summary>
        /// <param name="path">The path of the JavaScript resource to be read.</param>
        /// <returns>The contents of <paramref cref="path" />.</returns>
        public static string ReadScript(string path)
        {
            return ReadScript(path, Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Reads a JavaScript resource from a given assembly.
        /// </summary>
        /// <param name="path">The path of the JavaScript resource to be read.</param>
        /// <param name="assembly">The <see cref="Assembly" /> from which the script should be loaded.</param>
        /// <returns>The contents of <paramref cref="path" />.</returns>
        internal static string ReadScript(string path, Assembly assembly)
        {
            return path[0].Equals('/')
                ? ReadAssemblyRelativeScript(assembly, path)
                : ReadAssemblyAbsoluteScript(assembly, path);
        }

        private static string ReadAssemblyRelativeScript(Assembly asm, string path)
        {
            return ReadAssemblyAbsoluteScript(asm, asm.GetName().Name + path.Replace("/", "."));
        }

        private static string ReadAssemblyAbsoluteScript(Assembly asm, string path)
        {
            using (var stream = asm.GetManifestResourceStream(path.Replace("/", ".")))
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
