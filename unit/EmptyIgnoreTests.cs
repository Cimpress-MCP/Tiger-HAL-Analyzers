// <copyright file="EmptyIgnoreTests.cs" company="Cimpress, Inc.">
//   Copyright 2020 Cimpress, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License") –
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>

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
        static readonly MetadataReference[] s_allAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray() ?? Array.Empty<MetadataReference>();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyIgnoreAnalyzer());

        [Fact(DisplayName = "An empty source code file produces no diagnostic.")]
        public static async Task EmptySourceCode_Empty()
        {
            var diagnostics = await Diagnose(string.Empty, "Empty.cs", "empty");

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A populated ignore transformation produces no diagnostic.")]
        public static async Task PopulatedIgnore_Empty()
        {
            const string Source = @"
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
            var diagnostics = await Diagnose(Source, "PopulatedIgnore.cs", "populatedignore");

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A populated ignore transformation produces no diagnostic.")]
        public static async Task PopulatedIgnore2_Empty()
        {
            const string Source = @"
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
            var diagnostics = await Diagnose(Source, "PopulatedIgnore2.cs", "populatedignore2");

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "An empty ignore transformation produces TH1003.")]
        public static async Task EmptyIgnore_TH1003()
        {
            const string Source = @"
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
            var diagnostics = await Diagnose(Source, "EmptyIgnore.cs", "emptyignore");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(EmptyIgnoreAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "An empty ignore transformation produces TH1003.")]
        public static async Task EmptyIgnore2_TH1003()
        {
            const string Source = @"
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
            var diagnostics = await Diagnose(Source, "EmptyIgnore2.cs", "emptyignore2");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(EmptyIgnoreAnalyzer.Id, diagnostic.Id);
        }

        static async Task<ImmutableArray<Diagnostic>> Diagnose(string source, string fileName, string projectName)
        {
            var projectId = ProjectId.CreateNewId(debugName: projectName);
            var documentId = DocumentId.CreateNewId(projectId, debugName: fileName);
            using var workspace = new AdhocWorkspace();
            var solution = workspace
                .CurrentSolution
                .AddProject(projectId, name: projectName, assemblyName: projectName, CSharp)
                .AddDocument(documentId, fileName, SourceText.From(source));
            var project = s_allAssemblies
                .Aggregate(solution, (agg, curr) => agg.AddMetadataReference(projectId, curr))
                .GetProject(projectId);
            return project switch
            {
                { } p => await p.GetCompilationAsync() switch
                {
                    { } c => await c.WithAnalyzers(s_analyzers).GetAnalyzerDiagnosticsAsync(),
                    null => ImmutableArray<Diagnostic>.Empty,
                },
                null => ImmutableArray<Diagnostic>.Empty,
            };
        }
    }
}
