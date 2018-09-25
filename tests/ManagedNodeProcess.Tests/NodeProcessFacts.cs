using System;
using System.Threading.Tasks;
using Xunit;

namespace ManagedNodeProcess.Tests
{
    public class NodeProcessFacts
    {
        [Fact]
        public void SimpleScriptChunksAreEvaluated()
        {
            using (var nodeProc = new NodeProcess())
            {
                nodeProc.Start();

                Assert.Equal(402, nodeProc.EvaluateScriptChunk<int>("200 + 202;"));
            }
        }

        [Fact]
        public void ScriptChunkChainsAreEvaluated()
        {
            using (var nodeProc = new NodeProcess())
            {
                nodeProc.Start();

                nodeProc.EvaluateScriptChunk("var x = 500;");

                Assert.Equal(702, nodeProc.EvaluateScriptChunk<int>("x + 202;"));
            }
        }

        [Fact]
        public void ScriptChunkChainsWithFunctionsAreEvaluated()
        {
            using (var nodeProc = new NodeProcess())
            {
                nodeProc.Start();

                nodeProc.EvaluateScriptChunk("var x = 800;");
                nodeProc.EvaluateScriptChunk("function getX() { return x; }");

                Assert.Equal(1002, nodeProc.EvaluateScriptChunk<int>("getX() + 202;"));
            }
        }

        [Fact]
        public void ScriptChunkAllowsJsonArguments()
        {
            using (var nodeProc = new NodeProcess())
            {
                nodeProc.Start();

                nodeProc.EvaluateScriptChunk("function sumJson(obj) { return obj.a + obj.b; }");

                Assert.Equal(30, nodeProc.EvaluateScriptChunk<int>("sumJson({ a: 10, b: 20 });"));
            }
        }

        [Fact]
        public void ScriptChunkReturnsJsonObject()
        {
            using (var nodeProc = new NodeProcess())
            {
                nodeProc.Start();

                nodeProc.EvaluateScriptChunk("function getJson() { return { a: 10, b: 20 }; }");

                var obj = nodeProc.EvaluateScriptChunk<dynamic>("getJson();");

                Assert.NotNull(obj);
                Assert.Equal(10, (int)(obj.a));
                Assert.Equal(20, (int)(obj.b));
            }
        }

        [Fact]
        public void SynchronousScriptChunkErrorsAreThrownAsApplicationExceptions()
        {
            const string errorMessage = "Some error message.";

            using (var nodeProc = new NodeProcess())
            {
                nodeProc.Start();

                var applicationErr = Assert.Throws<ApplicationException>(() =>
                {
                    nodeProc.EvaluateScriptChunk($"throw new Error('{errorMessage}');");
                });
                
                Assert.Equal(errorMessage, applicationErr.Message);
            }
        }

        [Fact]
        public async Task AsynchronousScriptChunkErrorsAreThrownAsApplicationExceptions()
        {
            const string errorMessage = "Some error message.";

            using (var nodeProc = new NodeProcess())
            {
                nodeProc.Start();
                
                try
                {
                    await nodeProc.EvaluateScriptChunkAsync<string>($"throw new Error('{errorMessage}');");
                }
                catch (ApplicationException applicationErr)
                {
                    Assert.Equal(errorMessage, applicationErr.Message);
                }
            }
        }
    }
}
