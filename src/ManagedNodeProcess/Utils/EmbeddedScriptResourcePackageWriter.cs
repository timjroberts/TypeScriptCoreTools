
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using ManagedNodeProcess.Packages;

namespace ManagedNodeProcess.Utils
{
    /// <summary>
    /// An <see cref="EmbeddedScriptResourcePackageWriter" /> implementation that creates Node.js importable
    /// packages from embedded JavaScript resources.
    /// </summary>
    public abstract class EmbeddedScriptResourcePackageWriter : IPackageWriter
    {
        private readonly IEnumerable<string> _paths;

        /// <summary>
        /// Initializes the base <see cref="EmbeddedScriptResourcePackageWriter" />
        /// </summary>
        /// <param name="paths">The paths to the embedded JavaScript resources.</param>
        protected EmbeddedScriptResourcePackageWriter(IEnumerable<string> paths)
        {
            _paths = paths;
        }

        /// <summary>
        /// Creates a package within a given directory.
        /// </summary>
        /// <param name="scriptAssembly">The assembly that contains the Package Writer.</param>
        /// <param name="directory">The directory in which the package should be created.</param>
        /// <returns>A <see cref="Task" /> object.</returns>
        public async Task WritePackage(Assembly scriptAssembly, DirectoryInfo directory)
        {
            foreach (var path in _paths)
            {
                using (var file = new StreamWriter(Path.Combine(directory.FullName, path.Split('/').AsEnumerable().LastOrDefault() ?? path)))
                {
                    await file.WriteAsync(EmbeddedScriptResourceReader.ReadScript(path, scriptAssembly));
                }
            }
        }
    }
}