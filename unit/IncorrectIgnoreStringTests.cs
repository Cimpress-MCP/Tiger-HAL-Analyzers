using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Tiger.Hal.Analyzers;
using Tiger.Types;
using Xunit;
using static Microsoft.CodeAnalysis.LanguageNames;

namespace Test
{
    /// <summary>Tests related to the <see cref="IncorrectIgnoreExpressionAnalyzer"/> class.</summary>
    public static class IncorrectIgnoreStringTests
    {
        static readonly MetadataReference[] s_allAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new IncorrectIgnoreStringAnalyzer());

        [Fact(DisplayName = "An empty source code file produces no diagnostic.")]
        public static async Task EmptySourceCode_Empty()
        {
            var diagnostics = await Diagnose(string.Empty, "Empty.cs", "empty").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A correct nameof selector produces no diagnostic.")]
        public static async Task CorrectNameOfSelector_Empty()
        {
            const string source = @"
using System;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class Linker
        {
            public Uri Id { get; set; }

            public Uri Link { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Ignore(nameof(Linker.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Correct.cs", "correct").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "An incorrect nameof selector produces TH1005.")]
        public static async Task IncorrectNameOfSelector_Empty()
        {
            const string source = @"
using System;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class Linker
        {
            public Uri Id { get; set; }

            public Uri Link { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Ignore(nameof(ComplicatedLinkingTests.Linker));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Incorrect.cs", "incorrect").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreStringAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "A literal string matching a property produces TH1005.")]
        public static async Task LiteralStringSelector_TH1005()
        {
            const string source = @"
using System;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class Linker
        {
            public Uri Id { get; set; }

            public Uri Link { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Ignore(""Link"");
        }
    }
}
";
            var diagnostics = await Diagnose(source, "LiteralString.cs", "literalstring").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreStringAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "A literal string not matching a property produces TH1005.")]
        public static async Task LiteralStringNotMatchSelector_TH1005()
        {
            const string source = @"
using System;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class Linker
        {
            public Uri Id { get; set; }

            public Uri Link { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Ignore(""Stromboli"");
        }
    }
}
";
            var diagnostics = await Diagnose(source, "LiteralString.cs", "literalstring").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreStringAnalyzer.Id, diagnostic.Id);
        }

        static Task<ImmutableArray<Diagnostic>> Diagnose(string source, string fileName, string projectName)
        {
            var projectId = ProjectId.CreateNewId(debugName: projectName);
            var documentId = DocumentId.CreateNewId(projectId, debugName: fileName);
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, name: projectName, assemblyName: projectName, CSharp)
                .AddDocument(documentId, fileName, SourceText.From(source));
            return s_allAssemblies
                .Aggregate(solution, (agg, curr) => agg.AddMetadataReference(projectId, curr))
                .GetProject(projectId)
                .GetCompilationAsync()
                .Map(c => c.WithAnalyzers(s_analyzers))
                .Bind(c => c.GetAnalyzerDiagnosticsAsync());
        }
    }
}
