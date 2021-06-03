// <copyright file="IncorrectIgnoreStringTests.cs" company="Cimpress, Inc.">
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
    /// <summary>Tests related to the <see cref="IncorrectIgnoreExpressionAnalyzer"/> class.</summary>
    public static class IncorrectIgnoreStringTests
    {
        static readonly MetadataReference[] s_allAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray() ?? Array.Empty<MetadataReference>();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new IncorrectIgnoreStringAnalyzer());

        [Fact(DisplayName = "An empty source code file produces no diagnostic.")]
        public static async Task EmptySourceCode_Empty()
        {
            var diagnostics = await Diagnose(string.Empty, "Empty.cs", "empty");

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A correct nameof selector produces no diagnostic.")]
        public static async Task CorrectNameOfSelector_Empty()
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
            transformationMap.Ignore(nameof(Linker.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(Source, "Correct.cs", "correct");

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A correct nameof selector produces no diagnostic.")]
        public static async Task CorrectNameOfSelector2_Empty()
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
            transformationMap.Ignore(nameof(Linker.Link));
        }
    }
}
";
            var diagnostics = await Diagnose(Source, "Correct2.cs", "correct2");

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "An incorrect nameof selector produces TH1005.")]
        public static async Task IncorrectNameOfSelector_Empty()
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
            transformationMap.Ignore(nameof(ComplicatedLinkingTests.Linker));
        }
    }
}
";
            var diagnostics = await Diagnose(Source, "Incorrect.cs", "incorrect");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreStringAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "An incorrect nameof selector produces TH1005.")]
        public static async Task IncorrectNameOfSelector2_Empty()
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
            transformationMap.Ignore(nameof(ComplicatedLinkingTests.Linker));
        }
    }
}
";
            var diagnostics = await Diagnose(Source, "Incorrect2.cs", "incorrect2");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreStringAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "A literal string matching a property produces TH1005.")]
        public static async Task LiteralStringSelector_TH1005()
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
            transformationMap.Ignore(""Link"");
        }
    }
}
";
            var diagnostics = await Diagnose(Source, "LiteralString.cs", "literalstring");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreStringAnalyzer.Id, diagnostic.Id);
        }

        [Fact(DisplayName = "A literal string not matching a property produces TH1005.")]
        public static async Task LiteralStringNotMatchSelector_TH1005()
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
            transformationMap.Ignore(""Stromboli"");
        }
    }
}
";
            var diagnostics = await Diagnose(Source, "LiteralString.cs", "literalstring");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(IncorrectIgnoreStringAnalyzer.Id, diagnostic.Id);
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
