Creating C# analyzers with the help of [Roslyn](https://github.com/dotnet/roslyn) is very easy. At first it seems a huge learning curve needed because of Roslyn and the complexity of compilers, but in reality, it is much simpler than expected.

There is one more advantage of creating analyzers. It give a perfect environment to practice [Test Driven Development (TDD)](https://en.wikipedia.org/wiki/Test-driven_development).

Before beginning we need to make sure, that the analyzer extensions are installed to Visual Studio, otherwise we will not have templates and Syntax Visualizer window. To [install the SDK](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/#installation-instructions) simply enable  .NET Compiler Platform SDK feature in Visual Studio.

In this post I will show how to create an analyzer, which checks if an [_if statement_](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/if-else) or a [_conditional operator_](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator) uses [equality comparison operator](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/equality-comparison-operator) to compare an operand with null. If that is the case, the analyzer will suggest a to replace the comparison with [is pattern matching](https://docs.microsoft.com/en-us/dotnet/csharp/pattern-matching) with nulls, which syntax is available with the latest C# version.

Let's take an example: when the following code is written, the analyzer should issue a warning and offer a code fix.

```csharp
if(o == null)
{
  return true;
}
```
When the user applies the code fix offered,  it should replace the equality comparison to:

```csharp
if(o is null)
{
  return true;
}
```

#### Let's create a Project ####

To create the project open **File** -> **New** -> **Project** -> **Extensibility** -> **Analyzer with Code Fix**

This will create 3 projects in the solution. One for the analyzer itself, one for the unit tests, and one for the VSIX extension. In this post I will not use the VSIX extension, I simply ignore/delete this project, I will show a different way for testing the analyzer.

#### Unit tests ####

With the context above, let's start by creating the unit tests. First, I will create a unit test for the analyzer, to make sure the right diagnostics are returned, then I will show a unit test for the code fix provider as well. I will not try cover all test cases in this post (though feel free to do so); this post will cover only one happy path in the tests.


The test project provides a ```DiagnosticVerifier``` base class with some helper methods ```VerifyCSharpDiagnostic```. To write a unit test, we only need to provide a test case, which is the code to test and an expected result, which is the type of ```DiagnosticResult```. 

>Note, that though the default template uses MSTests it can be easily replaced to say [xUnit](https://xunit.github.io/). Just add the required nuget packages, remove the MSTests packages and replace ```TestMethod``` attributes with ```Fact```. If the [xunit test runner](https://www.nuget.org/packages/xunit.runner.visualstudio/) package is also installed, the Visual Studio should be able to run tests from the tests window.

Let's look at the first test case:

```csharp
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
```


In this test case, the test variable string provides the code to be analyzed. The pattern we are looking for is at line 12, ```o == null```. We also define a ```DiagnosticResult``` with some predefined const string messages and Ids. Notice, that the error's message is created from a format string, which has one parameter the ```o``` identifier. Finally, the test uses the VerifyCSharpDiagnostic method to verify the expected behavior.


In a similar way, for the code verification, we can derive from the ```CodeFixVerifier``` class and use the VerifyCSharpFix method to verify the fix. In this case the input string and the expected result:

```csharp
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
```

These fixtures will also need to override two methods to return our code fix provider and analyzer:

```csharp
protected override CodeFixProvider GetCSharpCodeFixProvider()
{
  return new IsNullPatternCodeFixProvider();
}
protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
{
  return new IsNullPatternAnalyzer();
}
```
At this point, these two tests should give an idea on how to add tests for the rest of the use-cases, which is beyond the scope of this post.

#### Creating the Analyzer ####

In this section will focus on how to create the analyzer. I create a class IsNullPatternAnalyzer, which derives from ```DiagnosticAnalyzer``` and has a ```[DiagnosticAnalyzer(LanguageNames.CSharp)]``` attribute, but this should be all provided by the sample.


To expose what kind of diagnostics we provide, we need to create a ```DiagnosticDescriptor``` and expose it to the runtime through an override of type ImmutableArray of DiagnosticDescriptors. Secondly, we also override the ```Initialize(...)``` method where we can register callback method for the runtime. This callback will be invoked once the runtime matches an Operation/Syntax/Symbol/etc. with the given OperationKind/SyntaxKind/SymbolKind. In the above use-case I register a callback for [conditional operations](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.iconditionaloperation?view=roslyn-dotnet).

```csharp
private static DiagnosticDescriptor IsNullRule = new DiagnosticDescriptor(IsNullRuleId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(IsNullRule); } }

public override void Initialize(AnalysisContext context) => context.RegisterOperationAction(AnalyzeOperation, OperationKind.Conditional);
```

Now we can implement the AnalyzeOperation method. This method takes a context of the operation found by the compiler. On the operation we could use the WhenTrue, WhenFalse properties but for now we are only interested in the Condition property. We retrieve the Syntax of the condition and with the ```IfRecognizer.UsesEqualsForNullCheck``` helper method we check if it is an equality comparison against ```null```. If this is satisfied, we create a diagnostic object instance and report it to the runtime through the ```ReportDiagnostic``` method call.

```csharp
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
```

>Note, that we can achieve the same analysis results if we implement a syntax analyzer instead of an Operation analyzer.


Finally, let's take a look at the ```UsesEqualsForNullCheck``` helper method. This method takes an ExpressionSyntax. We check if it is a binary expression, and if it is an equals expression. If so, we retrieve both operands and check if either of them is ```null```, while we return the name of the other operand as an out parameter. This identifier will be used later by the CodeFixProvider.

```csharp
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
```

#### Code Fix Provider ####

To create a code fix provider, we need to derive from ```CodeFixProvider``` type, and add an attribute, ```ExportCodeFixProvider```. In similar fashion to the Analyzer, we override a couple of methods: returning which diagnostic rules are handled, and a method ```RegisterCodeFixesAsync```, that registers another method ```ReplaceWithIsPattern``` to be invoked once the user asks for a preview of the fix, or the fix to be applied on the user's codebase.


```csharp
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
  var binaryExpression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();
  context.RegisterCodeFix(
    CodeAction.Create(
      IsNullPatternAnalyzer.Title,
      cancellation => ReplaceWithIsPattern(context.Document, binaryExpression, cancellation),
      IsNullPatternAnalyzer.Title),
    diagnostic);
}
```

In the ```RegisterCodeFixesAsync``` method we can use the location of the diagnostic issue and the FindToken method to retrieve the BinaryExpressionSyntax in questions to be fixed. Then the ```ReplaceWithIsPattern``` method is invoked to create a fix:

```csharp
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
```

The above method takes the binary operation and uses the ```UsesEqualsForNullCheck``` helper method to validate that we are actually looking at an equals binary expression against a ```null```. It also returns the identifier for the other operand of the binary expression. It creates a new syntax using the SyntaxFactory helper class and the retrieved identifier. This is the place where **View** -> **Other Windows** -> **Syntax Visualizer** window gives us some help. If we write some code that is equivalent to the expected result, we can visualize the syntax tree of the expected code. 
Finally, as Roslyn works with immutable data structures, we create a new syntax tree by replacing the original binary expression with the generated ```IsPatternExpression```.

#### Testing the Analyzer and Code Fix Provider ####

To test the solution (other than unit testing), we can either use the given VSIX project, or an easier way to is create a nuget package from the csproj containing the analyzer and code fix provider. For this we only need to right click on the IsNullAnalyzer project and select **Pack**. This will create a nupkg file in the bin\debug folder. At this point we can move this file or  [local nuget repository](https://docs.microsoft.com/en-us/nuget/hosting-packages/local-feeds) and install it from there to our test application. Note, that for this to work there is a tools folder with an install.ps1 file given by the template project. This file is also referenced in the csproj of the analyzer
```
<ItemGroup>
    <None Update="tools\*.ps1" CopyToOutputDirectory="Always" Pack="true" PackagePath="" />
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
</ItemGroup>
```
so it gets packaged up. When we install the nuget package this will make sure that our analyzer is installed properly.
