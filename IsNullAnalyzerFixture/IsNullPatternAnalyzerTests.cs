using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace IsAnalyzer.Test
{
  [TestClass]
  public class IsNullPatternAnalyzerTests : DiagnosticVerifier
  {
    [TestMethod]
    public void GivenIfEqualsNull_Analyze_DiagnosticsErrorReturned()
    {
      var test = @"
namespace ConsoleApplication1
{
    public class TestClass
    {
        public bool TestMethod()
        {
            var o = new object();
            if(o == null)
            {
                return true;
            }
            return false;
        }
    }
}";
      var expected = new DiagnosticResult
      {
        Id = IsNullPatternAnalyzer.IsNullRuleId,
        Message = string.Format(IsNullPatternAnalyzer.MessageFormat, "o"),
        Severity = DiagnosticSeverity.Warning,
        Locations = new[]
        {
          new DiagnosticResultLocation("Test0.cs", 9, 16)
        }
      };

      VerifyCSharpDiagnostic(test, expected);
    }

    [TestMethod]
    public void GivenConditionalExpression_Analyze_DiagnosticErrorReturned()
    {
      var test = @"
namespace ConsoleApplication1
{
    public class TestClass
    {
        public bool TestMethod()
        {
            var o = new object();
            var result = o == null ? true : false;
            return result;
        }
    }
}";
      var expected = new DiagnosticResult
      {
        Id = IsNullPatternAnalyzer.IsNullRuleId,
        Message = string.Format(IsNullPatternAnalyzer.MessageFormat, "o"),
        Severity = DiagnosticSeverity.Warning,
        Locations = new[]
        {
          new DiagnosticResultLocation("Test0.cs", 9, 26)
        }
      };

      VerifyCSharpDiagnostic(test, expected);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
      return new IsNullPatternAnalyzer();
    }
  }
}
