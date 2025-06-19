using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace TransactionalSessionAnalyzer.Test
{
    [TestClass]
    public class IMessageSessionAnalyzerTests
    {
        [TestMethod]
        public void TestAnalyzerExists()
        {
            var analyzer = new IMessageSessionAnalyzer();
            Assert.IsNotNull(analyzer);
            Assert.IsTrue(analyzer.SupportedDiagnostics.Length > 0);
            Assert.AreEqual("TSA001", analyzer.SupportedDiagnostics[0].Id);
        }

        [TestMethod]
        public async Task AnalyzerDetectsIMessageSessionInParameter()
        {
            var source = @"
using System;
using NServiceBus;

public class TestClass
{
    public void TestMethod(IMessageSession session)
    {
    }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("TSA001", diagnostics[0].Id);
            Assert.IsTrue(diagnostics[0].GetMessage().Contains("ITransactionalSession"));
        }

        [TestMethod]
        public async Task AnalyzerDetectsIMessageSessionInField()
        {
            var source = @"
using System;
using NServiceBus;

public class TestClass
{
    private IMessageSession _session;
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("TSA001", diagnostics[0].Id);
        }

        [TestMethod]
        public async Task AnalyzerDetectsIMessageSessionInProperty()
        {
            var source = @"
using System;
using NServiceBus;

public class TestClass
{
    public IMessageSession Session { get; set; }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("TSA001", diagnostics[0].Id);
        }

        [TestMethod]
        public async Task AnalyzerDetectsIMessageSessionInVariableDeclaration()
        {
            var source = @"
using System;
using NServiceBus;

public class TestClass
{
    public void TestMethod()
    {
        IMessageSession session = null;
    }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("TSA001", diagnostics[0].Id);
        }

        [TestMethod]
        public async Task AnalyzerDetectsMultipleIMessageSessionUsages()
        {
            var source = @"
using System;
using NServiceBus;

public class TestClass
{
    private IMessageSession _session;
    public IMessageSession Session { get; set; }
    
    public void TestMethod(IMessageSession session)
    {
        IMessageSession localSession = null;
    }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(4, diagnostics.Length);
            Assert.IsTrue(diagnostics.All(d => d.Id == "TSA001"));
        }

        [TestMethod]
        public async Task AnalyzerDoesNotDetectITransactionalSession()
        {
            var source = @"
using System;
using NServiceBus;

public class TestClass
{
    private ITransactionalSession _session;
    public ITransactionalSession Session { get; set; }
    
    public void TestMethod(ITransactionalSession session)
    {
        ITransactionalSession localSession = null;
    }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(0, diagnostics.Length);
        }

        [TestMethod]
        public async Task AnalyzerDoesNotDetectUnrelatedTypes()
        {
            var source = @"
using System;

public class TestClass
{
    private string _session;
    public int Session { get; set; }
    
    public void TestMethod(object session)
    {
        var localSession = ""test"";
    }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(0, diagnostics.Length);
        }

        [TestMethod]
        public async Task AnalyzerHandlesNestedClasses()
        {
            var source = @"
using System;
using NServiceBus;

public class OuterClass
{
    public class InnerClass
    {
        private IMessageSession _session;
        
        public void TestMethod(IMessageSession session)
        {
            IMessageSession localSession = null;
        }
    }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(3, diagnostics.Length);
            Assert.IsTrue(diagnostics.All(d => d.Id == "TSA001"));
        }

        [TestMethod]
        public async Task AnalyzerHandlesInterfaceImplementation()
        {
            var source = @"
using System;
using NServiceBus;

public interface ITestInterface
{
    void TestMethod(IMessageSession session);
}

public class TestClass : ITestInterface
{
    public void TestMethod(IMessageSession session)
    {
    }
}";

            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(2, diagnostics.Length);
            Assert.IsTrue(diagnostics.All(d => d.Id == "TSA001"));
        }

        private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            var analyzer = new IMessageSessionAnalyzer();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            
            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            return diagnostics.Where(d => d.Id == "TSA001").ToArray();
        }
    }

    [TestClass]
    public class IMessageSessionCodeFixProviderTests
    {
        [TestMethod]
        public void TestCodeFixProviderExists()
        {
            var codeFixProvider = new IMessageSessionCodeFixProvider();
            Assert.IsNotNull(codeFixProvider);
            Assert.IsTrue(codeFixProvider.FixableDiagnosticIds.Contains("TSA001"));
        }

        [TestMethod]
        public void TestCodeFixProviderSupportsCorrectDiagnosticId()
        {
            var codeFixProvider = new IMessageSessionCodeFixProvider();
            Assert.IsTrue(codeFixProvider.FixableDiagnosticIds.Contains("TSA001"));
        }

        [TestMethod]
        public void TestCodeFixProviderHasFixAllProvider()
        {
            var codeFixProvider = new IMessageSessionCodeFixProvider();
            var fixAllProvider = codeFixProvider.GetFixAllProvider();
            Assert.IsNotNull(fixAllProvider);
        }

        // Note: More comprehensive code fix testing would require a more complex test setup
        // with document models and syntax transformations. For now, we test the basic structure.
    }

    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task AnalyzerAndCodeFixProviderWorkTogether()
        {
            var source = @"
using System;
using NServiceBus;

public class TestClass
{
    public void TestMethod(IMessageSession session)
    {
    }
}";

            // Test that analyzer detects the issue
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("TSA001", diagnostics[0].Id);

            // Test that code fix provider can handle the diagnostic
            var codeFixProvider = new IMessageSessionCodeFixProvider();
            Assert.IsTrue(codeFixProvider.FixableDiagnosticIds.Contains(diagnostics[0].Id));
        }

        [TestMethod]
        public async Task AnalyzerPerformanceWithLargeFile()
        {
            var sourceBuilder = new System.Text.StringBuilder();
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using NServiceBus;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("public class TestClass");
            sourceBuilder.AppendLine("{");

            // Generate many method declarations to test performance
            for (int i = 0; i < 100; i++)
            {
                sourceBuilder.AppendLine($"    public void TestMethod{i}(IMessageSession session{i}) {{ }}");
            }

            sourceBuilder.AppendLine("}");

            var source = sourceBuilder.ToString();
            var diagnostics = await GetDiagnosticsAsync(source);
            
            Assert.AreEqual(100, diagnostics.Length);
            Assert.IsTrue(diagnostics.All(d => d.Id == "TSA001"));
        }

        private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            var analyzer = new IMessageSessionAnalyzer();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            
            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            return diagnostics.Where(d => d.Id == "TSA001").ToArray();
        }
    }
}
