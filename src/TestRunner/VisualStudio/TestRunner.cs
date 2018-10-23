using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json.Linq;

namespace TypeScript.Sdk.TestRunner.VisualStudio
{
    [FileExtension(".dll")]
    [DefaultExecutorUri("executor://typescript-sdk/netcoreapp/typescript")]
    [ExtensionUri("executor://typescript-sdk/netcoreapp/typescript")]
    public class TestRunner : ITestDiscoverer, ITestExecutor
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            foreach (var testCase in GetTestCasesFromSources(sources))
            {
                discoverySink.SendTestCase(testCase);
            }
        }

        public void Cancel()
        { }

        public void RunTests(IEnumerable<TestCase> testCases, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var filter = new TestCaseFilter(runContext, frameworkHandle);
            var filteredTestCases = testCases.Where(testCase => filter.MatchTestCase(testCase));

            using (var jestProcess = new JestNodeProcess())
            {
                jestProcess.Start();

                var results = jestProcess.RunTests(
                    new JArray(filteredTestCases.GroupBy(testCase => testCase.CodeFilePath)
                        .Select(groupedTestCase =>
                        {
                            var groupedTestCaseObj = new JObject();

                            groupedTestCaseObj["codeFilePath"] = groupedTestCase.Key;
                            groupedTestCaseObj["testCases"] = new JArray(
                                groupedTestCase.Select(testCase =>
                                {
                                    var testCaseObj = new JObject();

                                    testCaseObj["fullyQualifiedName"] = testCase.FullyQualifiedName;
                                    testCaseObj["codeFilePath"] = testCase.CodeFilePath;
                                    testCaseObj["source"] = testCase.Source;
                                    testCaseObj["displayName"] = testCase.DisplayName;
                                    testCaseObj["lineNumber"] = testCase.LineNumber;

                                    return testCaseObj;
                                })
                            );

                            return groupedTestCaseObj;
                        })
                    )
                );

                foreach (var resultObj in results)
                {
                    var resultTestCase = testCases.First(testCase => string.Equals(testCase.FullyQualifiedName, (string)resultObj["testCase"]["fullyQualifiedName"], StringComparison.CurrentCulture));
                    var result = new TestResult(resultTestCase);

                    result.Outcome = string.Equals((string)resultObj["outcome"], "passed", StringComparison.CurrentCulture)
                        ? TestOutcome.Passed
                        : TestOutcome.Failed;

                    result.ErrorMessage = (string)resultObj["errorMessage"];

                    frameworkHandle.RecordResult(result);
                }
            }
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            RunTests(GetTestCasesFromSources(sources), runContext, frameworkHandle);
        }

        private static IEnumerable<TestCase> GetTestCasesFromSources(IEnumerable<string> sources)
        {
            using (var jestProcess = new JestNodeProcess())
            {
                jestProcess.Start();

                var discoveredTests = jestProcess.DiscoverTests(new JArray(sources));

                return discoveredTests.Select(discoveredTest => ToTestCase(discoveredTest));
            }
        }

        private static TestCase ToTestCase(JToken obj)
        {
            return new TestCase((string)obj["fullyQualifiedName"], new Uri("executor://typescript-sdk/netcoreapp/typescript"), (string)obj["source"])
            {
                DisplayName = (string)obj["displayName"],
                LineNumber = (int)obj["lineNumber"],
                CodeFilePath = (string)obj["codeFilePath"]
            };
        }
    }
}