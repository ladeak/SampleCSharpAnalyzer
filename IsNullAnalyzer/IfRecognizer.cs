using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IsAnalyzer
{
  public static class IfRecognizer
  {
    public static bool UsesEqualsForNullCheck(ExpressionSyntax expressionSyntax, out IdentifierNameSyntax identifierName)
    {
      if(expressionSyntax is BinaryExpressionSyntax bops && bops.Kind() == SyntaxKind.EqualsExpression)
      {
        if(bops.Left is LiteralExpressionSyntax leftLiteral && leftLiteral.Kind() == SyntaxKind.NullLiteralExpression)
        {
          identifierName = bops.Right as IdentifierNameSyntax;
          return identifierName != null;
        }
        else if(bops.Right is LiteralExpressionSyntax rightLiteral && rightLiteral.Kind() == SyntaxKind.NullLiteralExpression)
        {
          identifierName = bops.Left as IdentifierNameSyntax;
          return identifierName != null;
        }
      }
      identifierName = null;
      return false;
    }
  }
}
