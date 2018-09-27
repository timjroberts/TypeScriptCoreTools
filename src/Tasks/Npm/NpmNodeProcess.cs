using System.Threading.Tasks;
using ManagedNodeProcess;
using ManagedNodeProcess.Utils;
using Newtonsoft.Json.Linq;

namespace Tasks.Npm
{
    /// <summary>
    /// A node process that be used to fulfill npm related actions.
    /// </summary>
    internal class NpmNodeProcess : NodeProcess
    {
        /// <summary>
        /// Initializes a new <see cref="NpmNodeProcess" /> object.
        /// </summary>
        public NpmNodeProcess()
            : base(new [] { EmbeddedScriptResourceReader.ReadScript("/Scripts/Npm.js") })
        { }

        /// <summary>
        /// Returns data about a resolved package.
        /// </summary>
        /// <param name="packageName">The name of the package to resolve.</param>
        /// <returns>A <see cref="JObject" /> that describes the resolved package; otherwise
        /// <c>null</c> if <paramref name="packageName" /> could not be resolved.</returns>
        public JObject ResolvePackage(string packageName)
        {
            return EvaluateScriptChunk<JObject>($"ResolvePackage('{packageName}');");
        }
    }
}