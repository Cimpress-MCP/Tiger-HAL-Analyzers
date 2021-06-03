// <copyright file="IncorrectHoistTests.cs" company="Cimpress, Inc.">
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
using static System.StringComparison;
using static Microsoft.CodeAnalysis.LanguageNames;

namespace Test
{
    /// <summary>Tests related to the <see cref="IncorrectHoistAnalyzer"/> class.</summary>
    public static class IncorrectHoistTests
    {
        static readonly MetadataReference[] s_allAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator)
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToArray() ?? Array.Empty<MetadataReference>();

        static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new IncorrectHoistAnalyzer());

        [Fact(DisplayName = "An empty source code file produces no diagnostic.")]
        public static async Task EmptySourceCode_Empty()
        {
            var diagnostics = await Diagnose(string.Empty, "Empty.cs", "empty");

            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A simple property selector produces no diagnostic.")]
        public static async Task SimplePropertySelector_Empty()
        {
            const string Source = @"
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
            var diagnostics = await Diagnose(Source, "Simple.cs", "simple");
            Assert.Empty(diagnostics);
        }

        [Fact(DisplayName = "A selector which is wrapped in a function produces TH1004.")]
        public static async Task FunctionCall_Extension_TH1004()
        {
            const string Source = @"
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
            var diagnostics = await Diagnose(Source, "WrappedInId.cs", "wrappedinid");

            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d => d.Id.StartsWith(IncorrectHoistAnalyzer.Id, Ordinal));
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
