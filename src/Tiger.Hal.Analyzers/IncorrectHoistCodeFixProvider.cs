// <copyright file="IncorrectHoistCodeFixProvider.cs" company="Cimpress, Inc.">
//   Copyright 2018 Cimpress, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
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
    /// <summary>Fixes incorrect invocations of Hoist.</summary>
    [ExportCodeFixProvider(CSharp, Name = nameof(IncorrectHoistCodeFixProvider)), Shared]
    public sealed class IncorrectHoistCodeFixProvider
        : CodeFixProvider
    {
        const string Title = "Remove Transformation";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IncorrectHoistAnalyzer.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var methodNameLocation = diagnostic.AdditionalLocations[0];
                var invocation = (InvocationExpressionSyntax)root.FindNode(methodNameLocation.SourceSpan);
                if (!(invocation.Expression is MemberAccessExpressionSyntax maes))
                {
                    return;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: Title,
                        createChangedDocument: _ => Remove(root, context.Document, invocation, maes.Expression),
                        equivalenceKey: Title),
                    diagnostic);
            }
        }

        Task<Document> Remove(SyntaxNode root, Document document, InvocationExpressionSyntax invocation, ExpressionSyntax leftOfTheDot)
        {
            var newRoot = root.ReplaceNode(invocation, leftOfTheDot);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
