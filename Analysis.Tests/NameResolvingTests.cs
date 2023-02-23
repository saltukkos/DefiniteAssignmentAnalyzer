using Analysis.InspectionDescriptors;
using LanguageModel;
using NUnit.Framework;

namespace Analysis.Tests;

[TestFixture]
public class NameResolvingTests
{
    [Test]
    public void Analyze_TwoVariablesWithSameName_InspectionReported()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("a"),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var a; {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "a"))}
");
    }

    [Test]
    public void Analyze_AssignmentOfUndeclaredVariable_InspectionReported()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new AssignVariable("b"),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
b = smth; {ValidationHelper.Error(s => new UnknownVariableDescriptor(s, "b"))}
");
    }

    [Test]
    public void Analyze_AssignmentOfConflictingVariable_OnlyNameConflictInspectionReported()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new VariableDeclaration("a"),
            new AssignVariable("a"),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var a; {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "a"))}
a = smth;
");
    }

    [Test]
    public void Analyze_FunctionHidesVariableWithSameNameFromOuterScope_InspectionReported()
    {
        var program = new Program
        {
            new VariableDeclaration("X"),
            new FunctionDeclaration("F")
            {
                Body =
                {
                    new FunctionDeclaration("X")
                }
            }
        };
        
        ValidationHelper.ValidateResult(program, $@"
var X;
func F {{
    func X {{
    }} {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "X"))}
}}
");
    }

    [Test]
    public void Analyze_TwoFunctionsWithSameName_InspectionReported()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo"),
            new FunctionDeclaration("Foo"),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
}}
func Foo {{
}} {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "Foo"))}
");
    }

    [Test]
    public void Analyze_FunctionAndVariableWithSameName_InspectionReported()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo"),
            new VariableDeclaration("Foo"),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
}}
var Foo; {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "Foo"))}
");
    }

    [Test]
    public void Analyze_FunctionAndVariableWithSameName_AssignmentAndPrintAndInvokeAreOk()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo"),
            new VariableDeclaration("Foo"),
            new AssignVariable("Foo"),
            new PrintVariable("Foo"),
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
}}
var Foo; {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "Foo"))}
Foo = smth;
print(Foo);
Foo();
");
    }

    [Test]
    public void Analyze_SameVariableInNestedContextAfterBaseContextDeclaration_InspectionReported()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new VariableDeclaration("a"),
                    new AssignVariable("a"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
func Foo {{
    var a; {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "a"))}
    a = smth;
}}

Foo();
");
    }

    [Test]
    public void Analyze_SameVariableInNestedContextBeforeBaseContextDeclaration_NoInspections()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new VariableDeclaration("a"),
                    new AssignVariable("a"),
                }
            },
            new VariableDeclaration("a"),
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, @"
func Foo {
    var a;
    a = smth;
}

var a;
Foo();
");
    }

    [Test]
    public void Analyze_NestedFunctionContainsFunctionWithSameName_InspectionProduced()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new FunctionDeclaration("Foo")
                    {
                        Body =
                        {
                            new VariableDeclaration("b"),
                            new AssignVariable("b"),
                        }
                    },
                    new Invocation("Foo", true),
                }
            },
            new Invocation("Foo", true),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
    func Foo {{
        var b;
        b = smth;
    }} {ValidationHelper.Error(s => new ConflictingIdentifierNameDescriptor(s, "Foo"))}

    if (smth) Foo();
}}

if (smth) Foo();
");
    }

    [Test]
    public void Analyze_SameDeclarationsInDifferentNestedContexts_NoInspections()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new FunctionDeclaration("Bar")
                    {
                        Body =
                        {
                            new VariableDeclaration("a"),
                            new AssignVariable("a")
                        }
                    },
                    new VariableDeclaration("a"),
                    new AssignVariable("a"),
                    new Invocation("Bar", false),
                }
            },
            new FunctionDeclaration("Foo2")
            {
                Body =
                {
                    new FunctionDeclaration("Bar")
                    {
                        Body =
                        {
                            new VariableDeclaration("a"),
                            new AssignVariable("a")
                        }
                    },
                    new VariableDeclaration("a"),
                    new AssignVariable("a"),
                    new Invocation("Bar", false),
                }
            },
            new Invocation("Foo", false),
            new Invocation("Foo2", false),
        };

        ValidationHelper.ValidateResult(program, @"
func Foo {
    func Bar {
        var a;
        a = smth;
    }
    var a;
    a = smth;
    Bar();
}

func Foo2 {
    func Bar {
        var a;
        a = smth;
    }
    var a;
    a = smth;
    Bar();
}

Foo();
Foo2();
");
    }
}