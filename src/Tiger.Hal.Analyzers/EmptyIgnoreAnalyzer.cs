// <copyright file="EmptyIgnoreAnalyzer.cs" company="Cimpress, Inc.">
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Microsoft.CodeAnalysis.LanguageNames;
using static Microsoft.CodeAnalysis.SymbolKind;
using static Microsoft.CodeAnalysis.WellKnownDiagnosticTags;

namespace Tiger.Hal.Analyzers
{
    /// <summary>Analyzes empty invocations of Ignore.</summary>
    [DiagnosticAnalyzer(CSharp)]
    public sealed class EmptyIgnoreAnalyzer
        : DiagnosticAnalyzer
    {
        /// <summary>The unique identifier of the rule associated with this analyzer.</summary>
        public const string Id = "TH1003";

        const string Ignore = "Ignore";

        static readonly DiagnosticDescriptor s_rule = new(
            id: Id,
            title: "Remove empty ignore transformation.",
            messageFormat: "Remove empty ignore transformation.",
            category: "FadeOut",
            defaultSeverity: Info,
            isEnabledByDefault: true,
            description: "Ignoring an empty collection of selectors incurs a slight runtime cost.",
            helpLinkUri: "https://github.com/Cimpress-MCP/Tiger-HAL-Analyzers/blob/master/doc/reference/TH1003_RemoveEmptyIgnoreTransformation.md",
            customTags: Unnecessary);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(csac =>
            {
                var containingType1 = csac.Compilation.GetTypeByMetadataName("Tiger.Hal.ITransformationMap`1");
                var containingType2 = csac.Compilation.GetTypeByMetadataName("Tiger.Hal.ITransformationMap`2");

                if (containingType1 is null && containingType2 is null)
                {
                    return;
                }

                csac.RegisterSyntaxNodeAction(snac => AnalyzeSyntaxNode(snac, containingType1, containingType2), InvocationExpression);
            });
        }

        static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, params INamedTypeSymbol?[] containingTypes)
        {
            var ies = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(ies, context.CancellationToken);
            if (symbolInfo.Symbol?.Kind is not Method)
            {
                return;
            }

            var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;
            if (!containingTypes.Contains(methodSymbol.ContainingType.OriginalDefinition) || methodSymbol.Name is not Ignore)
            {
                return;
            }

            if (ies.ArgumentList.Arguments.Count is not 0)
            {
                // note(cosborn) We only fire if there are no arguments.
                return;
            }

            if (ies.Expression is not MemberAccessExpressionSyntax maes)
            {
                // todo(cosborn) Or else what? What would this look like?
                return;
            }

            var dotLocation = maes.OperatorToken.GetLocation();
            var argumentsLocation = ies.ArgumentList.GetLocation();
            var deletableLocation = Location.Create(
                context.SemanticModel.SyntaxTree,
                TextSpan.FromBounds(dotLocation.SourceSpan.Start, argumentsLocation.SourceSpan.End));

            context.ReportDiagnostic(Diagnostic.Create(s_rule, deletableLocation));
        }
    }
}
