// <copyright file="IncorrectHoistAnalyzer.cs" company="Cimpress, Inc.">
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
    /// <summary>Analyzes incorrect invocations of Hoist.</summary>
    [DiagnosticAnalyzer(CSharp)]
    public sealed class IncorrectHoistAnalyzer
        : DiagnosticAnalyzer
    {
        /// <summary>The unique identifier of the rule associated with this analyzer.</summary>
        public const string Id = "TH1004";

        const string Hoist = "Hoist";

        static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: Id,
            title: "Selector argument must be a simple property selector.",
            messageFormat: "Remove meaningless hoist transformation.",
            category: "FadeOut",
            defaultSeverity: Error,
            isEnabledByDefault: true,
            description: "Hoisting anything but a simple property selector will fail at runtime. The property name is used to determine the value to ignore in the output.",
            helpLinkUri: "https://github.com/Cimpress-MCP/Tiger-HAL-Analyzers/blob/master/doc/reference/TH1004_SelectorArgumentMustBeASimplePropertySelector.md",
            customTags: Unnecessary);

        static readonly DiagnosticDescriptor s_ruleFadeOut = new DiagnosticDescriptor(
            id: Id + "_fadeout",
            title: s_rule.Title,
            messageFormat: s_rule.MessageFormat,
            category: "FadeOut",
            defaultSeverity: Hidden,
            isEnabledByDefault: s_rule.IsEnabledByDefault,
            helpLinkUri: s_rule.HelpLinkUri,
            customTags: Unnecessary);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_ruleFadeOut);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var containingType = compilationContext.Compilation.GetTypeByMetadataName("Tiger.Hal.ITransformationMap`1");
                if (containingType is null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, containingType), InvocationExpression);
            });
        }

        void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol containingType)
        {
            var ies = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(ies, context.CancellationToken);
            if (symbolInfo.Symbol?.Kind != Method)
            {
                return;
            }

            var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;
            if (methodSymbol.ContainingType.OriginalDefinition != containingType || methodSymbol.Name != Hoist)
            {
                return;
            }

            if (!(ies.Expression is MemberAccessExpressionSyntax maes))
            {
                // todo(cosborn) Or else what? What would this look like?
                return;
            }

            var selectorLocation = Locate.NonSimpleSelector(context, ies.ArgumentList.Arguments[0].Expression);
            if (selectorLocation is null)
            {
                // note(cosborn) Congratulations, it's a simple selector.
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, selectorLocation, additionalLocations: ImmutableArray.Create(ies.GetLocation())));

            var dotLocation = maes.OperatorToken.GetLocation();
            var argumentsLocation = ies.ArgumentList.GetLocation();
            var deletableLocation = Location.Create(
                context.SemanticModel.SyntaxTree,
                TextSpan.FromBounds(dotLocation.SourceSpan.Start, argumentsLocation.SourceSpan.End));

            context.ReportDiagnostic(Diagnostic.Create(s_ruleFadeOut, deletableLocation));
        }
    }
}
