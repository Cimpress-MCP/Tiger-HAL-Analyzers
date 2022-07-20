// <copyright file="Locate.cs" company="Cimpress, Inc.">
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tiger.Hal.Analyzers
{
    /// <summary>Utility functions.</summary>
    public static class Locate
    {
        /// <summary>Checks if the provided syntax does not represent a simple selector, reporting its location if so.</summary>
        /// <param name="context">The context for syntax node analysis.</param>
        /// <param name="expressionSyntax">The expression to check.</param>
        /// <returns>The location of the simple selector, if one was found; otherwise, null.</returns>
        public static Location? NonSimpleSelector(SyntaxNodeAnalysisContext context, ExpressionSyntax expressionSyntax)
        {
            if (expressionSyntax is null)
            {
                throw new ArgumentNullException(nameof(expressionSyntax));
            }

            if (expressionSyntax is not LambdaExpressionSyntax les)
            {
                return expressionSyntax.GetLocation();
            }

            var parameterSyntax = les switch
            {
                SimpleLambdaExpressionSyntax { Parameter: { } p } => p,
                ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: { } p } => p[0],
                _ => null,
            };

            if (parameterSyntax is not { } ps)
            {
                return null;
            }

            if (GetIdentifierSymbol(context, les.Body) is { } @is)
            {
                var parameterSymbol = context.SemanticModel.GetDeclaredSymbol(ps, context.CancellationToken);
                if (SymbolEqualityComparer.Default.Equals(@is, parameterSymbol))
                {
                    return null;
                }
            }

            return expressionSyntax.GetLocation();

            static ISymbol? GetIdentifierSymbol(SyntaxNodeAnalysisContext c, CSharpSyntaxNode body) => body switch
            {
                MemberAccessExpressionSyntax maes when maes.Expression is IdentifierNameSyntax ins => c.SemanticModel.GetSymbolInfo(ins, c.CancellationToken).Symbol,
                CastExpressionSyntax ces => GetIdentifierSymbol(c, ces.Expression),
                _ => null,
            };
        }
    }
}
