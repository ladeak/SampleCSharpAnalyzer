using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace IsAnalyzer
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class IsNullPatternAnalyzer : DiagnosticAnalyzer
  {
    public const string IsNullRuleId = "IsNullAnalyzer";

    public const string Title = "Use 'is' pattern";
    public const string MessageFormat = "Compare {0} with 'is' pattern.";
    public const string Description = "Use 'is' pattern to compare with null";
    public const string Category = "Convention";

    private static DiagnosticDescriptor IsNullRule = new DiagnosticDescriptor(IsNullRuleId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(IsNullRule); } }

    public override void Initialize(AnalysisContext context) => context.RegisterOperationAction(AnalyzeOperation, OperationKind.Conditional);

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
      if(context.Operation is IConditionalOperation conditionalOperation
        && conditionalOperation.Condition != null)
      {
        if(conditionalOperation.Condition.Syntax is ExpressionSyntax expression
        && IfRecognizer.UsesEqualsForNullCheck(expression, out var otherIndentifer))
        {
          var diagnostic = Diagnostic.Create(IsNullRule, expression.GetLocation(), otherIndentifer.Identifier.ValueText);
          context.ReportDiagnostic(diagnostic);
        }
      }
    }
  }
}
