using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IsAnalyzer
{
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IsNullPatternCodeFixProvider)), Shared]
  public class IsNullPatternCodeFixProvider : CodeFixProvider
  {
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
      get { return ImmutableArray.Create(IsNullPatternAnalyzer.IsNullRuleId); }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
      return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

      var diagnostic = context.Diagnostics.First();
      var diagnosticSpan = diagnostic.Location.SourceSpan;

      var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();

      context.RegisterCodeFix(
        CodeAction.Create(
          IsNullPatternAnalyzer.Title,
          cancellation => ReplaceWithIsPattern(context.Document, declaration, cancellation),
          IsNullPatternAnalyzer.Title),
        diagnostic);
    }

    private async Task<Document> ReplaceWithIsPattern(Document document, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
      if(IfRecognizer.UsesEqualsForNullCheck(expression, out var otherIndentifer))
      {
        var newSyntax = SyntaxFactory.IsPatternExpression(otherIndentifer,
          SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
        );
        var root = await document.GetSyntaxRootAsync();
        return document.WithSyntaxRoot(root.ReplaceNode(expression, newSyntax));
      }
      return document;
    }
  }
}
