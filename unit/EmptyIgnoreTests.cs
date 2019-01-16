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
    /// <summary>Tests related to the <see cref="EmptyIgnoreAnalyzer"/> class.</summary>
    public static class EmptyIgnoreTests
    {
        static readonly MetadataReference[] s_allAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyIgnoreAnalyzer());

        [Fact(DisplayName = "An empty source code file produces no diagnostic.")]
        public static async Task EmptySourceCode_Empty()
        {
            var diagnostics = await Diagnose(string.Empty, "Empty.cs", "empty").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A populated ignore transformation produces no diagnostic.")]
        public static async Task PopulatedIgnore_Empty()
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
            transformationMap.Link(""wow"", l => l.Link).Ignore(l => l.Id);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "PopulatedIgnore.cs", "populatedignore").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A populated ignore transformation produces no diagnostic.")]
        public static async Task PopulatedIgnore2_Empty()
        {
            const string source = @"
using System;
using System.Collections.ObjectModel;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class LinkerCollection
          : Collection<Linker>
        {
        }
        
        public sealed class Linker
        {
            public Uri Id { get; set; }

            public Uri Link { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Link(""wow"", l => l.Link).Ignore(l => l.Id);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "PopulatedIgnore2.cs", "populatedignore2").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "An empty ignore transformation produces TH1003.")]
        public static async Task EmptyIgnore_TH1003()
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
            transformationMap.Link(""wow"", l => l.Link).Ignore();
        }
    }
}
";
            var diagnostics = await Diagnose(source, "EmptyIgnore.cs", "emptyignore").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(EmptyIgnoreAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "An empty ignore transformation produces TH1003.")]
        public static async Task EmptyIgnore2_TH1003()
        {
            const string source = @"
using System;
using System.Collections.ObjectModel;
using Tiger.Hal;

namespace Test
{
    public static class ComplicatedLinkingTests
    {
        public sealed class LinkerCollection
          : Collection<Linker>
        {
        }
        
        public sealed class Linker
        {
            public Uri Id { get; set; }

            public Uri Link { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Link(""wow"", l => l.Link).Ignore();
        }
    }
}
";
            var diagnostics = await Diagnose(source, "EmptyIgnore2.cs", "emptyignore2").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(EmptyIgnoreAnalyzer.Id, diagnostic.Id);
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
