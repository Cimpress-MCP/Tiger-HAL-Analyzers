// <copyright file="IncorrectLinkAndIgnoreCodeFixProvider.cs" company="Cimpress, Inc.">
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

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.LanguageNames;

namespace Tiger.Hal.Analyzers
{
    /// <summary>Fixes incorrect invocations of LinkAndIgnore.</summary>
    [ExportCodeFixProvider(CSharp, Name = nameof(IncorrectLinkAndIgnoreCodeFixProvider))]
    [Shared]
    public sealed class IncorrectLinkAndIgnoreCodeFixProvider
        : CodeFixProvider
    {
        const string Title = "Link without Ignoring";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IncorrectLinkAndIgnoreAnalyzer.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                var methodLocation = diagnostic.AdditionalLocations[0];
                var methodName = (IdentifierNameSyntax)root.FindNode(methodLocation.SourceSpan);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: Title,
                        createChangedDocument: _ => Unignore(root, context.Document, methodName),
                        equivalenceKey: Title),
                    diagnostic);
            }
        }

        static Task<Document> Unignore(SyntaxNode root, Document document, IdentifierNameSyntax linkAndIgnore)
        {
            var link = SyntaxFactory.IdentifierName("Link");
            var newRoot = root.ReplaceNode(linkAndIgnore, link);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
