using System;
using System.Threading.Tasks;
using ManagedNodeProcess;
using ManagedNodeProcess.Packages;
using ManagedNodeProcess.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Tasks.WebPack
{
    [RequiresNpmPackage("webpack", Version="4.6.0")]
    [RequiresNpmPackage("webpack-node-externals", Version="1.7.2")]
    [RequiresNpmPackage("source-map-loader", Version="0.2.4")]
    internal class WebPackNodeProcess : NodeProcess
    {
        public WebPackNodeProcess()
            : base(new [] { EmbeddedScriptResourceReader.ReadScript("/Scripts/WebPack.js") })
        { }

        public void PackProject(string libraryName, bool bundleAsLibrary, string configuration, JArray bundledPackages)
        {
            try
            {
                Task.Run(() => PackProjectAsync(libraryName, bundleAsLibrary, configuration, bundledPackages)).Wait();
            }
            catch (AggregateException aggErr)
            {
                foreach (var err in aggErr.Flatten().InnerExceptions)
                {
                    if (err is ApplicationException || err is PackageInstallException)
                    {
                        throw err;
                    }
                }

                throw new SystemException("Encountered an unknown error while evaluating the script chunk.");
            }
        }

        public async Task PackProjectAsync(string libraryName, bool bundleAsLibrary, string configuration, JArray bundledPackages)
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.None
            };

            await EvaluateScriptChunkAsync($"PackProject('{libraryName}', {bundleAsLibrary.ToString().ToLowerInvariant()}, '{configuration}', {JsonConvert.SerializeObject(bundledPackages, settings)});");
        }
    }
}