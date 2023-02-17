using Analysis.InspectionDescriptors;
using LanguageModel;
using NUnit.Framework;

namespace Analysis.Tests;

[TestFixture]
public sealed class DefiniteAssignmentRecursionTests
{
    [Test]
    public void Analyze_TwoLocalFunctionsCallEachOtherConditionally_AllVariableAccessesAreAnalyzed()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("b"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                    new Invocation("Bar", true),
                }
            },
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new Invocation("Foo", true),
                    new PrintVariable("b"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var b;
func Foo {{
    print(a);
    if (smth) Bar();
}}

func Bar {{
    if (smth) Foo();
    print(b);
}}

Foo(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "a"), new(s, "b")})}
");
    }

    [Test]
    public void Analyze_FunctionCallsItselfConditionally_AllVariableAccessesAreAnalyzed()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("b"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                    new Invocation("Foo", true),
                    new PrintVariable("b"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var b;
func Foo {{
    print(a);
    if (smth) Foo();
    print(b);
}}

Foo(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "a"), new(s, "b")})}
");
    }

    [Test]
    public void Analyze_FunctionCallsItselfUnconditionally_AllVariableAccessesAreAnalyzed()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("b"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                    new Invocation("Foo", false),
                    new PrintVariable("b"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var b;
func Foo {{
    print(a);
    Foo();
    print(b);
}}

Foo(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "a"), new(s, "b")})}
");
    }

    [Test(Description =
        "We can change this behaviour to be more consistent with non-self recursion calls with little tweaks if need")]
    public void Analyze_FunctionCallsItselfUnconditionally_LocalVariableAssignmentsAreIgnored()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new VariableDeclaration("a"),
                    new VariableDeclaration("b"),
                    new PrintVariable("a"),
                    new Invocation("Foo", false),
                    new PrintVariable("b"),
                    new AssignVariable("a"),
                    new AssignVariable("b"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
    var a;
    var b;
    print(a); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
    Foo();
    print(b); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "b"))}
    a = smth;
    b = smth;
}}

Foo();
");
    }

    [Test]
    public void Analyze_FunctionCallsItselfUnconditionally_LocalVariableAssignmentsAreConsideredInExternalContext()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new Invocation("Foo", false),
                    new AssignVariable("a"),
                }
            },
            new Invocation("Foo", false),
            new PrintVariable("a"),
        };

        ValidationHelper.ValidateResult(program, @"
var a;

func Foo {
    Foo();
    a = smth;
}

Foo();
print(a);
");
    }

    [Test]
    public void Analyze_FunctionCallsItselfUnconditionally_VariableAssignmentsAreConsideredAfterTheCall()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("b"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                    new Invocation("Foo", false),
                    new PrintVariable("b"),
                    new AssignVariable("a"),
                    new AssignVariable("b"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var b;
func Foo {{
    print(a);
    Foo();
    print(b);
    a = smth;
    b = smth;
}}

Foo(); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
");
    }

    [Test]
    public void Analyze_UseUnassignedVariableAfterRecursiveCall_InspectionProduced()
    {
        var program = new Program
        {
            new Invocation("Bar", false),
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new Invocation("Bar", false),
                    new PrintVariable("a"),
                }
            },
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new Invocation("Foo", true),
                }
            },
        };

        ValidationHelper.ValidateResult(program, $@"
Bar(); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
var a;

func Foo {{
    Bar();
    print(a);
}}

func Bar {{
    if (smth) Foo();
}}
");
    }

    [Test]
    public void Analyze_UseUnassignedVariableInRecursiveCallAndInitialize_InspectionProducedOnlyOnFirstInvocation()
    {
        var program = new Program
        {
            new Invocation("Bar", false),
            new Invocation("Bar", false),
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                    new Invocation("Bar", false),
                }
            },
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new Invocation("Foo", true),
                    new AssignVariable("a"),
                }
            },
        };

        ValidationHelper.ValidateResult(program, $@"
Bar(); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
Bar();

var a;

func Foo {{
    print(a);
    Bar();
}}

func Bar {{
    if (smth) Foo();
    a = smth;
}}
");
    }

    [Test]
    public void Analyze_UseOneOfRecursiveCallsOutsideOfFirstRecursion_AllVariablesAccessesAreConsidered()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("b"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new Invocation("Bar", true),
                    new PrintVariable("a"),
                }
            },
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new Invocation("Foo", true),
                    new PrintVariable("b"),
                }
            },
            new Invocation("Foo", false),
            new Invocation("Bar", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var b;
func Foo {{
    if (smth) Bar();
    print(a);
}}

func Bar {{
    if (smth) Foo();
    print(b);
}}

Foo(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "a"), new(s, "b")})}
Bar();
");
    }

    [Test]
    public void Analyze_UseOneOfNestedRecursiveCallsOutsideOfFirstRecursion_AllVariablesAccessesAreConsidered()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("b"),
            new VariableDeclaration("c"),
            new VariableDeclaration("d"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new Invocation("Bar", true),
                    new PrintVariable("a"),
                }
            },
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new Invocation("Foo", true),
                    new PrintVariable("b"),
                }
            },
            new FunctionDeclaration("Baz")
            {
                Body =
                {
                    new Invocation("Qux", true),
                    new Invocation("Bar", true),
                    new PrintVariable("c"),
                }
            },
            new FunctionDeclaration("Qux")
            {
                Body =
                {
                    new Invocation("Foo", true),
                    new Invocation("Baz", true),
                    new PrintVariable("d"),
                }
            },
            new Invocation("Foo", false),
            new Invocation("Baz", false),
            new Invocation("Qux", false),
            new Invocation("Bar", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var b;
var c;
var d;

func Foo {{
    if (smth) Bar();
    print(a);
}}

func Bar {{
    if (smth) Foo();
    print(b);
}}

func Baz {{
    if (smth) Qux();
    if (smth) Bar();
    print(c);
}}

func Qux {{
    if (smth) Foo();
    if (smth) Baz();
    print(d);
}}

Foo(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "a"), new(s, "b")})}
Baz(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "c"), new(s, "d")})}
Qux();
Bar();
");
    }
}