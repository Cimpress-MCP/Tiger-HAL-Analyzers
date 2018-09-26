// <copyright file="IncorrectIgnoreStringAnalyzer.cs" company="Cimpress, Inc.">
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
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Microsoft.CodeAnalysis.LanguageNames;
using static Microsoft.CodeAnalysis.SpecialType;
using static Microsoft.CodeAnalysis.SymbolKind;

namespace Tiger.Hal.Analyzers
{
    /// <summary>Analyzes incorrect invocations of Ignore with strings.</summary>
    [DiagnosticAnalyzer(CSharp)]
    public sealed class IncorrectIgnoreStringAnalyzer
        : DiagnosticAnalyzer
    {
        /// <summary>The unique identifier of the rule associated with this analyzer.</summary>
        public const string Id = "TH1005";

        const string Ignore = "Ignore";

        const string NameOf = "nameof";

        static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            id: Id,
            title: "Selector argument must be a name on the transforming type.",
            messageFormat: "Change parameter to use a valid nameof expression.",
            category: "Usage",
            defaultSeverity: Warning,
            isEnabledByDefault: true,
            description: "Ignoring a value that is not the name of a member on the transforming type incurs a runtime cost for no benefit.",
            helpLinkUri: "https://github.com/Cimpress-MCP/Tiger-HAL-Analyzers/blob/master/doc/reference/TH1005_SelectorArgumentMustBeANameOnTheTransformingType.md");

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

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
            if (methodSymbol.ContainingType.OriginalDefinition != containingType || methodSymbol.Name != Ignore)
            {
                return;
            }

            foreach (var argument in ies.ArgumentList.Arguments)
            {
                var namedThing = GetArgumentToNameOf(argument.Expression, context);
                if (namedThing is MemberAccessExpressionSyntax maes
                    && maes.Expression is IdentifierNameSyntax ins)
                {
                    var potentialTypeSymbolInfo = context.SemanticModel.GetSymbolInfo(ins, context.CancellationToken);
                    if (potentialTypeSymbolInfo.Symbol?.Kind == NamedType)
                    {
                        var typeSymbol = (INamedTypeSymbol)potentialTypeSymbolInfo.Symbol;
                        if (typeSymbol == symbolInfo.Symbol.ContainingType.TypeArguments[0])
                        {
                            continue;
                        }
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(s_rule, argument.GetLocation()));
            }

            ExpressionSyntax GetArgumentToNameOf(ExpressionSyntax expression, SyntaxNodeAnalysisContext c)
            {
                if (!expression.IsKind(InvocationExpression))
                {
                    return null;
                }

                var invocation = (InvocationExpressionSyntax)expression;
                if (invocation.ArgumentList.Arguments.Count != 1)
                {
                    return null;
                }

                if (!(invocation.Expression is IdentifierNameSyntax ins) || ins.Identifier.ValueText != NameOf)
                {
                    return null;
                }

                // note(cosborn) A nameof expression has no symbol because it doesn't have a definition, but it is typed as string.
                if (c.SemanticModel.GetSymbolInfo(expression, c.CancellationToken).Symbol != null
                    || c.SemanticModel.GetTypeInfo(expression, c.CancellationToken).Type?.SpecialType != System_String)
                {
                    return null;
                }

                return invocation.ArgumentList.Arguments[0].Expression;
            }
        }
    }
}
