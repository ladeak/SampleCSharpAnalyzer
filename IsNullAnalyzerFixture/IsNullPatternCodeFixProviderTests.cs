using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace IsAnalyzer.Test
{
  [TestClass]
  public class IsNullPatternCodeFixProviderTests : CodeFixVerifier
  {
    [TestMethod]
    public void GivenIfEquals_CodeFix_ReturnsIsNull()
    {
      var test = @"
namespace ConsoleApplication1
{
    public class TestClass
    {
        public bool TestMethod()
        {
            var o = new object();
            if(null==o)
            {
                return true;
            }
            return false;
        }
    }
}";

      var expectedFix = @"
namespace ConsoleApplication1
{
    public class TestClass
    {
        public bool TestMethod()
        {
            var o = new object();
            if(o is null)
            {
                return true;
            }
            return false;
        }
    }
}";
      
      VerifyCSharpFix(test, expectedFix);
    }

    [TestMethod]
    public void GivenConditionalEquals_CodeFix_ReturnsIsNull()
    {
      var test = @"
namespace ConsoleApplication1
{
    public class TestClass
    {
        public bool TestMethod()
        {
            var o = new object();
            return o == null ? true : false;
        }
    }
}";

      var expectedFix = @"
namespace ConsoleApplication1
{
    public class TestClass
    {
        public bool TestMethod()
        {
            var o = new object();
            return o is null ? true : false;
        }
    }
}";

      VerifyCSharpFix(test, expectedFix);
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
    {
      return new IsNullPatternCodeFixProvider();
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
      return new IsNullPatternAnalyzer();
    }
  }
}
