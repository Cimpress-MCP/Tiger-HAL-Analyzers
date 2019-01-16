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
using static System.StringComparison;

namespace Test
{
    /// <summary>Tests related to the <see cref="IncorrectHoistAnalyzer"/> class.</summary>
    public static class IncorrectHoistTests
    {
        static readonly MetadataReference[] s_allAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new IncorrectHoistAnalyzer());

        [Fact(DisplayName = "An empty source code file produces no diagnostic.")]
        public static async Task EmptySourceCode_Empty()
        {
            var diagnostics = await Diagnose(string.Empty, "Empty.cs", "empty").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A simple property selector produces no diagnostic.")]
        public static async Task SimplePropertySelector_Empty()
        {
            const string source = @"
using System;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class Linker
            : Collection<string>
        {
            public Uri Id { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Hoist(l => l.Id);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);
            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A selector which is wrapped in a function produces TH1004.")]
        public static async Task FunctionCall_Extension_TH1004()
        {
            const string source = @"
using System;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class Linker
            : Collection<string>
        {
            public Uri Id { get; set; }
        }

        static T Id<T>(T value) => value;

        public static void Property_Ignored(ITransformationMap<Linker, string> transformationMap)
        {
            transformationMap.Hoist(l => Id(l.Id));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "WrappedInId.cs", "wrappedinid").ConfigureAwait(false);

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectHoistAnalyzer.Id, Ordinal));
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
