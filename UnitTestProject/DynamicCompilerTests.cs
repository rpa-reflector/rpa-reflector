using Microsoft.VisualStudio.TestTools.UnitTesting;
using RPAReflector;
using System.CodeDom.Compiler;

namespace UnitTestProject
{
    /// <summary>
    /// Tests for DynamicCompiler.
    /// These tests exercise the runtime C# compilation pipeline without requiring
    /// any HiX/ChipSoft assemblies — only the standard .NET Framework DLLs.
    /// </summary>
    [TestClass]
    public class DynamicCompilerTests
    {
        // Minimal valid RequestHandler class that satisfies the convention
        // expected by CompileImplementation (a class named "RequestHandler").
        private const string ValidRequestHandlerSource = @"
            using System;
            public class RequestHandler
            {
                public string WebRootPath { get; set; }
                public string MsgControlId { get; set; }

                public string Execute(string input)
                {
                    return ""ok:"" + input;
                }
            }";

        // --- GetImplementationCompilationParameters ---

        [TestMethod]
        public void GetImplementationCompilationParameters_GenerateInMemory_IsTrue()
        {
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();

            Assert.IsTrue(parameters.GenerateInMemory);
        }

        [TestMethod]
        public void GetImplementationCompilationParameters_ContainsStandardAssemblies()
        {
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();

            CollectionAssert.Contains(parameters.ReferencedAssemblies, "mscorlib.dll");
            CollectionAssert.Contains(parameters.ReferencedAssemblies, "System.dll");
            CollectionAssert.Contains(parameters.ReferencedAssemblies, "System.Core.dll");
            CollectionAssert.Contains(parameters.ReferencedAssemblies, "System.Net.Http.dll");
        }

        // --- CompileImplementation: success path ---

        [TestMethod]
        public void CompileImplementation_ValidCode_RegistersRequestHandlerType()
        {
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();
            string moduleName = "TestModule_Register_" + System.Guid.NewGuid().ToString("N");

            DynamicCompiler.CompileImplementation(moduleName, ValidRequestHandlerSource, parameters, "1.0");

            Assert.IsTrue(
                DynamicCompiler.RequestHandlerTypes.ContainsKey(moduleName),
                "RequestHandlerTypes should contain the compiled module after successful compilation");
        }

        [TestMethod]
        public void CompileImplementation_ValidCode_StoresVersionWithPrefix()
        {
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();
            string moduleName = "TestModule_Version_" + System.Guid.NewGuid().ToString("N");

            DynamicCompiler.CompileImplementation(moduleName, ValidRequestHandlerSource, parameters, "2.5");

            Assert.AreEqual("v2.5", DynamicCompiler.ImplementationVersions[moduleName]);
        }

        [TestMethod]
        public void CompileImplementation_NullVersion_StoresUnknown()
        {
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();
            string moduleName = "TestModule_NullVer_" + System.Guid.NewGuid().ToString("N");

            DynamicCompiler.CompileImplementation(moduleName, ValidRequestHandlerSource, parameters, null);

            Assert.AreEqual("Unknown", DynamicCompiler.ImplementationVersions[moduleName]);
        }

        [TestMethod]
        public void CompileImplementation_ValidCode_ReturnsNonNullAssembly()
        {
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();
            string moduleName = "TestModule_Assembly_" + System.Guid.NewGuid().ToString("N");

            var assembly = DynamicCompiler.CompileImplementation(moduleName, ValidRequestHandlerSource, parameters, "1.0");

            Assert.IsNotNull(assembly);
        }

        [TestMethod]
        public void CompileImplementation_SourceWithNoRequestHandlerClass_DoesNotRegisterType()
        {
            const string sourceWithOtherClass = @"
                public class SomeOtherClass
                {
                    public int Value { get; set; }
                }";

            var parameters = DynamicCompiler.GetImplementationCompilationParameters();
            string moduleName = "TestModule_NoHandler_" + System.Guid.NewGuid().ToString("N");

            DynamicCompiler.CompileImplementation(moduleName, sourceWithOtherClass, parameters, "1.0");

            Assert.IsFalse(
                DynamicCompiler.RequestHandlerTypes.ContainsKey(moduleName),
                "A module without a class named 'RequestHandler' should not be registered");
        }

        // --- CompileImplementation: error path ---

        [TestMethod]
        public void CompileImplementation_InvalidSyntax_ThrowsCompilationException()
        {
            const string brokenSource = "this is not valid C# code !!!";
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();
            string moduleName = "TestModule_Error_" + System.Guid.NewGuid().ToString("N");

            Assert.ThrowsException<DynamicCompiler.CompilationException>(() =>
                DynamicCompiler.CompileImplementation(moduleName, brokenSource, parameters, "1.0"));
        }

        [TestMethod]
        public void CompileImplementation_InvalidSyntax_ExceptionContainsErrors()
        {
            const string brokenSource = "class Broken { void Oops( }";
            var parameters = DynamicCompiler.GetImplementationCompilationParameters();
            string moduleName = "TestModule_ErrDetails_" + System.Guid.NewGuid().ToString("N");

            DynamicCompiler.CompilationException ex = null;
            try
            {
                DynamicCompiler.CompileImplementation(moduleName, brokenSource, parameters, "1.0");
            }
            catch (DynamicCompiler.CompilationException caught)
            {
                ex = caught;
            }

            Assert.IsNotNull(ex);
            Assert.IsNotNull(ex.errors);
            Assert.IsTrue(ex.errors.Count > 0, "CompilationException should expose the compiler error collection");
        }

        // --- CompilationException ---

        [TestMethod]
        public void CompilationException_StoresErrorCollection()
        {
            var errors = new CompilerErrorCollection();
            errors.Add(new CompilerError("file.cs", 1, 1, "CS0001", "Test error"));

            var ex = new DynamicCompiler.CompilationException("test", errors);

            Assert.AreEqual(1, ex.errors.Count);
            Assert.AreEqual("CS0001", ex.errors[0].ErrorNumber);
        }

        // --- CompileEnvProvider: embedded resource ---

        [TestMethod]
        public void CompileEnvProvider_Hix61_EmbeddedResourceExists_AndIsNonEmpty()
        {
            var assembly = typeof(DynamicCompiler).Assembly;
            const string resourceName = "RPAReflector.EnvProviderTemplates.EnvProvider61.cs";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                Assert.IsNotNull(stream, "Embedded resource EnvProvider61.cs must exist in the RPAReflector assembly");
                using (var reader = new System.IO.StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    Assert.IsFalse(string.IsNullOrWhiteSpace(content), "EnvProvider61.cs embedded resource must have content");
                }
            }
        }

        [TestMethod]
        public void CompileEnvProvider_Hix63_EmbeddedResourceExists_AndIsNonEmpty()
        {
            var assembly = typeof(DynamicCompiler).Assembly;
            const string resourceName = "RPAReflector.EnvProviderTemplates.EnvProvider63.cs";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                Assert.IsNotNull(stream, "Embedded resource EnvProvider63.cs must exist in the RPAReflector assembly");
                using (var reader = new System.IO.StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    Assert.IsFalse(string.IsNullOrWhiteSpace(content), "EnvProvider63.cs embedded resource must have content");
                }
            }
        }

        // --- CompileEnvProvider: compilation is attempted ---
        // The HiX assemblies are not available in the test environment so compilation
        // will fail, but a CompilationException (not a NullReferenceException) proves
        // that the resource was successfully read and its content was passed to the compiler.

        [TestMethod]
        public void CompileEnvProvider_Hix61_ReadsResourceAndAttemptsCompilation()
        {
            var strategy = HixVersionStrategy.Create("6.1");
            var parameters = new CompilerParameters { GenerateInMemory = true };

            DynamicCompiler.CompilationException ex = null;
            try
            {
                DynamicCompiler.CompileEnvProvider(parameters, strategy, "TEST");
            }
            catch (DynamicCompiler.CompilationException caught)
            {
                ex = caught;
            }

            Assert.IsNotNull(ex, "Expected a CompilationException due to missing HiX assembly references");
            Assert.IsTrue(ex.errors.Count > 0, "Compiler errors must be present, confirming the source code was passed to CompileSourceCode");
        }

        [TestMethod]
        public void CompileEnvProvider_Hix63_ReadsResourceAndAttemptsCompilation()
        {
            var strategy = HixVersionStrategy.Create("6.3");
            var parameters = new CompilerParameters { GenerateInMemory = true };

            DynamicCompiler.CompilationException ex = null;
            try
            {
                DynamicCompiler.CompileEnvProvider(parameters, strategy, "TEST");
            }
            catch (DynamicCompiler.CompilationException caught)
            {
                ex = caught;
            }

            Assert.IsNotNull(ex, "Expected a CompilationException due to missing HiX assembly references");
            Assert.IsTrue(ex.errors.Count > 0, "Compiler errors must be present, confirming the source code was passed to CompileSourceCode");
        }
    }
}
