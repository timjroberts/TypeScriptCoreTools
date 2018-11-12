
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedNodeProcess;
using ManagedNodeProcess.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace TypeScript.Sdk.TestRunner.VisualStudio
{
    [RequiresNpmPackage("jest", Version="23.6.0")]
    [RequiresNpmPackage("jest-cli", Version="23.6.0")]
    [RequiresNpmPackage("typescript", Version="3.1.3")]
    [RequiresNpmPackage("node-glob", Version="1.2.0")]
    [RequiresNpmPackage("source-map-support", Version="0.5.9")]
    [RequiresNpmPackage("typescript-sdk-jest-resolve", PackageWriter=typeof(JestResolvePackageWriter))]
    [RequiresNpmPackage("typescript-sdk-jest-resolver", PackageWriter=typeof(JestResolverPackageWriter))]
    [RequiresNpmPackage("typescript-sdk-utils", PackageWriter=typeof(UtilsPackageWriter))]
    internal class JestNodeProcess : NodeProcess
    {
        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.None
        };

        public JestNodeProcess()
            : base(new [] { EmbeddedScriptResourceReader.ReadScript("TestRunner.VisualStudio/Scripts/JestTest.js") })
        { }

        public JArray DiscoverTests(JArray sourceFilePaths)
        {
            return EvaluateScriptChunk<JArray>($"DiscoverTests({JsonConvert.SerializeObject(sourceFilePaths, DefaultSerializerSettings)});");
        }

        public JArray RunTests(JArray groupedTestCases)
        {
            return EvaluateScriptChunk<JArray>($"RunTests({JsonConvert.SerializeObject(groupedTestCases, DefaultSerializerSettings)});");
        }
    }
}