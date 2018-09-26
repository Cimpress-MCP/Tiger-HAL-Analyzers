// <copyright file="Locate.cs" company="Cimpress, Inc.">
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
        public static Location NonSimpleSelector(SyntaxNodeAnalysisContext context, ExpressionSyntax expressionSyntax)
        {
            if (expressionSyntax is LambdaExpressionSyntax les)
            {
                ParameterSyntax parameterSyntax;
                switch (les)
                {
                    case SimpleLambdaExpressionSyntax sles:
                        parameterSyntax = sles.Parameter;
                        break;
                    case ParenthesizedLambdaExpressionSyntax ples:
                        // note(cosborn) Due to the signature, there can be only one.
                        parameterSyntax = ples.ParameterList.Parameters[0];
                        break;
                    default:
                        return null;
                }

                var identifierSymbol = GetIdentifierSymbol(context, les.Body);
                if (identifierSymbol != null)
                {
                    var parameterSymbol = context.SemanticModel.GetDeclaredSymbol(parameterSyntax, context.CancellationToken);
                    if (identifierSymbol == parameterSymbol)
                    {
                        return null;
                    }
                }
            }

            return expressionSyntax.GetLocation();

            ISymbol GetIdentifierSymbol(SyntaxNodeAnalysisContext c, CSharpSyntaxNode body)
            {
                switch (body)
                {
                    case MemberAccessExpressionSyntax maes when maes.Expression is IdentifierNameSyntax ins:
                        return c.SemanticModel.GetSymbolInfo(ins, c.CancellationToken).Symbol;
                    case CastExpressionSyntax ces:
                        return GetIdentifierSymbol(c, ces.Expression);
                    default:
                        return null;
                }
            }
        }
    }
}
