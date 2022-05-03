using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.Compiler.CSharp.UnitTests
{
    [TestClass]
    public class UnitTest_CompilerService
    {
        [TestMethod]
        public void Test_CompileCodeStr()
        {
            var codeStr = File.ReadAllText("./TestClasses/Contract2.cs", Encoding.UTF8);
            var res = CompilerService.Compile(codeStr);

            Assert.AreEqual(8, res.Manifest.Properties.Count());
        }

        [TestMethod]
        public void Test_CompileFile()
        {
            CompilerService.Compile(new string[] { "./TestClasses/Contract2.cs" }, "./TestClasses/", "ContractTest", false, false, false, false, ProtocolSettings.Default.AddressVersion);
            var nefFilePath = "./TestClasses/ContractTest.nef";
            Assert.IsTrue(File.Exists(nefFilePath));
            var manifestPath = "./TestClasses/ContractTest.manifest.json";
            Assert.IsTrue(File.Exists(manifestPath));

            File.Delete(nefFilePath);
            File.Delete(manifestPath);
        }
    }
}
