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
    /// <summary>Tests related to the
    /// <see cref="IncorrectLinkAndIgnoreAnalyzer"/> class and the
    /// <see cref="IncorrectLinkAndIgnoreCodeFixProvider"/> class.
    /// </summary>
    public static class IncorrectLinkAndIgnoreTests
    {
        static readonly MetadataReference[] s_allAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new IncorrectLinkAndIgnoreAnalyzer());

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
            transformationMap.LinkAndIgnore(""wow"", l => l.Link);
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
            TransformationMapExtensions.LinkAndIgnore(transformationMap, ""wow"", l => l.Link);
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
            transformationMap.LinkAndIgnore(""wow"", l => (Uri)l.Link);
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
            TransformationMapExtensions.LinkAndIgnore(transformationMap, ""wow"", l => (Uri)l.Link);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "Simple.cs", "simple").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A selector which is wrapped in a function produces TH1001.")]
        public static async Task FunctionCall_Extension_TH1001()
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
            transformationMap.LinkAndIgnore(""wow"", l => Id(l.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "WrappedInId.cs", "wrappedinid").ConfigureAwait(false);

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectLinkAndIgnoreAnalyzer.Id, Ordinal));
        }

        [Fact(DisplayName = "A selector which is wrapped in a function produces TH1001.")]
        public static async Task FunctionCall_Ordinary_TH1001()
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
            TransformationMapExtensions.LinkAndIgnore(transformationMap, ""wow"", l => Id(l.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "WrappedInId.cs", "wrappedinid").ConfigureAwait(false);

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectLinkAndIgnoreAnalyzer.Id, Ordinal));
        }

        [Fact(DisplayName = "A nested selector produces TH1001.")]
        public static async Task MultiProperty_Extension_TH1001()
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
            transformationMap.LinkAndIgnore(""wow"", l => l.Inner.Outer);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "MultiProperty.cs", "multiproperty").ConfigureAwait(false);

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectLinkAndIgnoreAnalyzer.Id, Ordinal));
        }

        [Fact(DisplayName = "A nested selector produces TH1001.")]
        public static async Task MultiProperty_Ordinary_TH1001()
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
            TransformationMapExtensions.LinkAndIgnore(transformationMap, ""wow"", l => l.Inner.Outer);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "MultiProperty.cs", "multiproperty").ConfigureAwait(false);

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectLinkAndIgnoreAnalyzer.Id, Ordinal));
        }

        [Fact(DisplayName = "A simple property selector with named arguments produces no diagnostic.")]
        public static async Task SimplePropertySelectorNamed_Extension_Empty()
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
            transformationMap.LinkAndIgnore(relation: ""wow"", selector: l => l.Link);
        }
    }
}
";
            var diagnostics = await Diagnose(source, "SimpleNamed.cs", "simplenamed").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A simple property selector with named, swapped arguments produces no diagnostic.")]
        public static async Task SimplePropertySelectorNamedSwapped_Extension_Empty()
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
            transformationMap.LinkAndIgnore(selector: l => l.Link, relation: ""wow"");
        }
    }
}
";
            var diagnostics = await Diagnose(source, "SimpleNamedSwapped.cs", "simplenamedswapped").ConfigureAwait(false);

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A selector which is wrapped in a function with named arguments produces TH1001.")]
        public static async Task FunctionCallNamed_Extension_TH1001()
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
            transformationMap.LinkAndIgnore(relation: ""wow"", selector: l => Id(l.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(source, "WrappedInIdNamed.cs", "wrappedinidnamed").ConfigureAwait(false);

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectLinkAndIgnoreAnalyzer.Id, Ordinal));
        }

        [Fact(DisplayName = "A selector which is wrapped in a function with named, swapped arguments produces TH1001.")]
        public static async Task FunctionCallNamedSwapped_Extension_TH1001()
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
            transformationMap.LinkAndIgnore(selector: l => Id(l.Link), relation: ""wow"");
        }
    }
}
";
            var diagnostics = await Diagnose(source, "WrappedInIdNamedSwapped.cs", "wrappedinidnamedswapped").ConfigureAwait(false);

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectLinkAndIgnoreAnalyzer.Id, Ordinal));
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
