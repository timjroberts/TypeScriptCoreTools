using System.Collections.Generic;
using ManagedNodeProcess.Utils;

namespace TypeScript.Sdk.TestRunner.VisualStudio
{
    internal class JestResolvePackageWriter : EmbeddedScriptResourcePackageWriter
    {
        public JestResolvePackageWriter()
            : base(new [] { "TestRunner.VisualStudio/Scripts/JestResolve/index.js" })
        { }
    }

    internal class JestResolverPackageWriter : EmbeddedScriptResourcePackageWriter
    {
        public JestResolverPackageWriter()
            : base(new [] { "TestRunner.VisualStudio/Scripts/JestResolver/index.js" })
        { }
    }

    internal class UtilsPackageWriter : EmbeddedScriptResourcePackageWriter
    {
        public UtilsPackageWriter() :
            base(new []
                { 
                    "TestRunner.VisualStudio/Scripts/Utils/index.js",
                    "TestRunner.VisualStudio/Scripts/Utils/TestCase.js",
                    "TestRunner.VisualStudio/Scripts/Utils/GroupedTestCase.js",
                    "TestRunner.VisualStudio/Scripts/Utils/PathUtils.js",
                    "TestRunner.VisualStudio/Scripts/Utils/TestDiscoverer.js",
                    "TestRunner.VisualStudio/Scripts/Utils/TestResult.js",
                    "TestRunner.VisualStudio/Scripts/Utils/TestRunner.js",
                }
            )
        { }
    }
}