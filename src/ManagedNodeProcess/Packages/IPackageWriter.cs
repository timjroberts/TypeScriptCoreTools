
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ManagedNodeProcess.Packages
{
    /// <summary>
    /// A utility for creating Node.js importable packages.
    /// </summary>
    /// <seealso cref="PackageManager" />
    /// <seealso cref="RequiresNpmPackageAttribute.PackageWriter" />
    public interface IPackageWriter
    {
        /// <summary>
        /// Creates a package within a given directory.
        /// </summary>
        /// <param name="scriptAssembly">The assembly that contains the Package Writer.</param>
        /// <param name="directory">The directory in which the package should be created.</param>
        /// <returns>A <see cref="Task" /> object.</returns>
        Task WritePackage(Assembly scriptAssembly, DirectoryInfo directory);
    }
}