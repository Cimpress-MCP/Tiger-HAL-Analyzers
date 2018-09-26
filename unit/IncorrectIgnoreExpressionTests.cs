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
    public static class IncorrectIgnoreExpressionTests
    {
        static readonly MetadataReference[] s_allAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new IncorrectIgnoreExpressionAnalyzer());

        [Fact(DisplayName = "An empty source code file produces no diagnostic.")]
        public static async Task EmptySourceCode_Empty()
        {
            var diagnostics = await Diagnose(string.Empty, "Empty.cs", "empty").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A simple property selector produces no diagnostic.")]
        public static async Task SimplePropertySelector_Extension_Empty()
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
            transformationMap.Ignore(l => l.Link);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);
            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A simple property selector produces no diagnostic.")]
        public static async Task SimplePropertySelector_Ordinary_Empty()
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
            TransformationMapExtensions.Ignore(transformationMap, l => l.Link);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A cast of a simple property selector produces no diagnostic.")]
        public static async Task CastSimplePropertySelector_Extension_Empty()
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
            transformationMap.Ignore(l => (Uri)l.Link);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A cast of a simple property selector produces no diagnostic.")]
        public static async Task CastSimplePropertySelector_Ordinary_Empty()
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
            TransformationMapExtensions.Ignore(transformationMap, l => (Uri)l.Link);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A block containing only a simple property selector produces no diagnostic.")]
        public static async Task BlockSimplePropertySelector_Extension_Empty()
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
            transformationMap.Ignore(l => { return l.Link; });
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A block containing only a simple property selector produces no diagnostic.")]
        public static async Task BlockSimplePropertySelector_Ordinary_Empty()
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
            TransformationMapExtensions.Ignore(transformationMap, l => { return l.Link; });
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A selector which is wrapped in a function produces TH1002.")]
        public static async Task FunctionCall_Extension_TH1002()
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

        static T Id<T>(T value) => value;

        public static void AnythingElse_NotIgnored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Ignore(l => Id(l.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "WrappedInId.cs", "wrappedinid").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreExpressionAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "A selector which is wrapped in a function produces TH1002.")]
        public static async Task FunctionCall_Ordinary_TH1002()
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

        static T Id<T>(T value) => value;

        public static void AnythingElse_NotIgnored(ITransformationMap<Linker> transformationMap)
        {
            TransformationMapExtensions.Ignore(transformationMap, l => Id(l.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "WrappedInId.cs", "wrappedinid").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreExpressionAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "A nested selector produces TH1002.")]
        public static async Task MultiProperty_Extension_TH1002()
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

            public Wow Inner { get; set; }
        }

        public sealed class Wow
        {
            public Uri Outer { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            transformationMap.Ignore(l => l.Inner.Outer);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "MultiProperty.cs", "multiproperty").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreExpressionAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "A nested selector produces TH1002.")]
        public static async Task MultiProperty_Ordinary_TH1002()
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

            public Wow Inner { get; set; }
        }

        public sealed class Wow
        {
            public Uri Outer { get; set; }
        }

        public static void Property_Ignored(ITransformationMap<Linker> transformationMap)
        {
            TransformationMapExtensions.Ignore(transformationMap, l => l.Inner.Outer);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "MultiProperty.cs", "multiproperty").ConfigureAwait(false);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreExpressionAnalyzer.Id, diagnostic.Id);
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
