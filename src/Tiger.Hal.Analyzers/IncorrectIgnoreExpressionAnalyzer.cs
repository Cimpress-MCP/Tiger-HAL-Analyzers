// <copyright file="IncorrectIgnoreExpressionAnalyzer.cs" company="Cimpress, Inc.">
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Microsoft.CodeAnalysis.LanguageNames;
using static Microsoft.CodeAnalysis.MethodKind;
using static Microsoft.CodeAnalysis.SymbolKind;

namespace Tiger.Hal.Analyzers
{
    /// <summary>Analyzes incorrect invocations of Ignore with expressions.</summary>
    [DiagnosticAnalyzer(CSharp)]
    public sealed class IncorrectIgnoreExpressionAnalyzer
        : DiagnosticAnalyzer
    {
        /// <summary>The unique identifier of the rule associated with this analyzer.</summary>
        public const string Id = "TH1002";

        const string Ignore = "Ignore";

        [SuppressMessage("Microsoft.Analysis", "IDE0090", Justification = "Analyzer does not understand target-typed new.")]
        static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: Id,
            title: "Selector argument must be a simple property selector",
            messageFormat: "Remove invalid selector from ignore transformation",
            category: "Usage",
            defaultSeverity: Error,
            isEnabledByDefault: true,
            description: "Ignoring anything but a simple property selector will fail at runtime. The property name is used to determine the value to ignore in the output.",
            helpLinkUri: "https://github.com/Cimpress-MCP/Tiger-HAL-Analyzers/blob/master/doc/reference/TH1002_SelectorArgumentMustBeASimplePropertySelector.md");

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
                if (csac.Compilation.GetTypeByMetadataName("Tiger.Hal.TransformationMapExtensions") is not { } ct)
                {
                    return;
                }

                csac.RegisterSyntaxNodeAction(snac => AnalyzeSyntaxNode(snac, ct), InvocationExpression);
            });
        }

        static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol containingType)
        {
            var ies = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(ies, context.CancellationToken);
            if (symbolInfo.Symbol?.Kind is not Method)
            {
                return;
            }

            var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;
            if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, containingType)
                || methodSymbol.Name is not Ignore)
            {
                return;
            }

            if (ies.Expression is not MemberAccessExpressionSyntax)
            {
                // todo(cosborn) Or else what? What would this look like?
                return;
            }

            int selectorSkip;
            switch (methodSymbol.MethodKind)
            {
                case Ordinary:
                    selectorSkip = 1;
                    break;
                case ReducedExtension:
                    selectorSkip = 0;
                    break;
                default:
                    return;
            }

            var selectorLocations = ies.ArgumentList.Arguments
                .Skip(selectorSkip)
                .Select(a => a.Expression)
                .Select(e => Locate.NonSimpleSelector(context, e))
                .OfType<Location>();
            foreach (var selectorLocation in selectorLocations)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, selectorLocation));
            }
        }
    }
}
