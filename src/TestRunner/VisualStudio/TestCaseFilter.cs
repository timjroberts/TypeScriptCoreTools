
using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TypeScript.Sdk.TestRunner.VisualStudio
{
    internal sealed class TestCaseFilter
    {
        private const string FullyQualifiedNameString = "FullyQualifiedName";

        private readonly IRunContext _runContext;
        private readonly bool _hasFilter;
        private readonly ITestCaseFilterExpression _filter;

        public TestCaseFilter(IRunContext runContext, IMessageLogger logger)
        {
            _runContext = runContext;

            _hasFilter = TryGetTestCaseFilterExpression(_runContext, logger, out _filter);
        }

        public bool MatchTestCase(TestCase testCase)
        {
            if (!_hasFilter)
            {
                // Had an error while getting filter, match no testcase to ensure discovered test list is empty
                return false;
            }
            else if (_filter == null)
            {
                // No filter specified, keep every testcase
                return true;
            }

            return _filter.MatchTestCase(testCase, (propertyName) => GetTestCasePropertyValue(testCase, propertyName));
        }

        private bool TryGetTestCaseFilterExpression(IRunContext runContext, IMessageLogger logger, out ITestCaseFilterExpression filter)
        {
            filter = null;

            try
            {
                filter = runContext.GetTestCaseFilter(new [] { FullyQualifiedNameString }, null);

                return true;
            }
            catch (TestPlatformFormatException e)
            {
                logger.SendMessage(TestMessageLevel.Warning, $"Exception filtering tests: {e.Message}");

                return false;
            }
        }

        private static object GetTestCasePropertyValue(TestCase testCase, string propertyName)
        {
            if (string.Equals(propertyName, FullyQualifiedNameString, StringComparison.OrdinalIgnoreCase)) return testCase.FullyQualifiedName;

            return null;
        }
    }
}