using Analysis.InspectionDescriptors;
using LanguageModel;
using NUnit.Framework;

namespace Analysis.Tests;

[TestFixture]
public sealed class DefiniteAssignmentBasicTests
{
    [Test]
    public void Analyze_NoLocalFunctionsCorrectUsage_NoInspectionsReported()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new AssignVariable("a"),
            new PrintVariable("a")
        };

        var analysisRunner = new AnalysisRunner();
        var result = analysisRunner.Analise(program);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Analyze_UseVariableBeforeAssign_UnassignedUsageReported()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new PrintVariable("a")
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
print(a); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
");
    }

    [Test]
    public void Analyze_UseVariableWithoutDeclaration_UnknownIdentifierReported()
    {
        var program = new Program
        {
            new AssignVariable("a"),
            new PrintVariable("a"),
        };

        ValidationHelper.ValidateResult(program, $@"
a = smth; {ValidationHelper.Error(s => new UnknownVariableDescriptor(s, "a"))}
print(a); {ValidationHelper.Error(s => new UnknownVariableDescriptor(s, "a"))}
");
    }

    [Test]
    public void Analyze_UsePrintVariableWithoutAssignAndDeclaration_InspectionReported()
    {
        var program = new Program
        {
            new PrintVariable("a"),
        };

        ValidationHelper.ValidateResult(program, $@"
print(a); {ValidationHelper.Error(s => new UnknownVariableDescriptor(s, "a"))}
");
    }

    [Test]
    public void Analyze_LocalFunctionDeclarationWithoutVariableDeclaration_InspectionReported()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                }
            },
            new VariableDeclaration("a"),
            new AssignVariable("a"),
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
    print(a); {ValidationHelper.Error(s => new UnknownVariableDescriptor(s, "a"))}
}}

var a;
a = smth;
Foo();
");
    }

    [Test]
    public void Analyze_LocalFunctionAccessToVariableBeforeFunctionDeclaration_NoInspections()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new AssignVariable("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, @"
var a;
a = smth;
func Foo {
    print(a);
}
Foo();
");
    }

    [Test]
    public void Analyze_LocalFunctionAccessToVariableBeforeFunctionInvocation_NoInspections()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                }
            },
            new AssignVariable("a"),
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, @"
var a;
func Foo {
    print(a);
}
a = smth;
Foo();
");
    }

    [Test]
    public void Analyze_LocalFunctionCallsAnotherLocalFunction_InitializationsArePropagated()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new AssignVariable("a"),
                    new PrintVariable("a"),
                }
            },
            new VariableDeclaration("b"),
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new Invocation("Foo", false),
                    new AssignVariable("b"),
                    new PrintVariable("a"),
                    new PrintVariable("b"),
                }
            },
            new Invocation("Bar", false),
            new PrintVariable("a"),
            new PrintVariable("b"),
        };

        ValidationHelper.ValidateResult(program, @"
var a;
func Foo {
    a = smth;
    print(a);
}

var b;
func Bar {
    Foo();
    b = smth;
    print(a);
    print(b);
}

Bar();
print(a);
print(b);
");
    }

    [Test]
    public void Analyze_LocalFunctionCallsAnotherLocalFunction_VariableAccessesArePropagated()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a")
                }
            },
            new VariableDeclaration("b"),
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new Invocation("Foo", false),
                    new PrintVariable("b"),
                }
            },
            new Invocation("Bar", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
func Foo {{
    print(a);
}}

var b;
func Bar {{
    Foo();
    print(b);
}}

Bar(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "a"), new(s, "b")})}
");
    }

    [Test]
    public void Analyze_DirectUnassignedVariableAccessInLocalFunction_InspectionReported()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new VariableDeclaration("a"),
                    new PrintVariable("a"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
    var a;
    print(a); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
}}

Foo();
");
    }

    [Test]
    public void Analyze_TransitiveUnassignedVariableAccessInLocalFunction_InspectionReported()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new VariableDeclaration("a"),
                    new Invocation("Bar", false),
                    new FunctionDeclaration("Bar")
                    {
                        Body =
                        {
                            new PrintVariable("a"),
                        }
                    },
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
    var a;
    Bar(); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}

    func Bar {{
        print(a);
    }}
}}

Foo();
");
    }

    [Test]
    public void Analyze_FunctionIsNeverCalled_InnerErrorsAreIgnored()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new VariableDeclaration("a"),
                    new Invocation("Bar", false),
                    new FunctionDeclaration("Bar")
                    {
                        Body =
                        {
                            new PrintVariable("a"),
                        }
                    },
                }
            },
        };

        ValidationHelper.ValidateResult(program, @"
func Foo {
    var a;
    Bar();

    func Bar {
        print(a);
    }
}
");
    }

    [Test]
    public void Analyze_CallIsConditional_AssignmentsAreNotConsideredAsDefinite()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new Invocation("Foo", true),
            new PrintVariable("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new AssignVariable("a"),
                }
            },
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
if (smth) Foo();
print(a); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}

func Foo {{
    a = smth;
}}
");
    }

    [Test]
    public void Analyze_CallIsConditional_NotAssignedVariablesAreReported()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new Invocation("Foo", true),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new PrintVariable("a"),
                }
            },
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
if (smth) Foo(); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
func Foo {{
    print(a);
}}
");
    }
    
    [Test]
    public void Analyze_VariableUsedMultipleTimesInNestedContexts_InspectionReportedOnce()
    {
        var program = new Program
        {
            new VariableDeclaration("a"),
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new Invocation("Bar", false),
                    new FunctionDeclaration("Bar")
                    {
                        Body =
                        {
                            new PrintVariable("a"),
                            new PrintVariable("a"),
                        }
                    },
                    new PrintVariable("a"),
                    new PrintVariable("a"),
                }
            },
            new Invocation("Foo", false),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
func Foo {{
    Bar();
    func Bar {{
        print(a);
        print(a);
    }}

    print(a);
    print(a);
}}

Foo(); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
");
    }

    [Test]
    public void Analyze_SameNameInDifferentContext_AssignmentsAreDifferent()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new Invocation("Bar", false),
                    new VariableDeclaration("a"),
                    new FunctionDeclaration("Bar")
                    {
                        Body =
                        {
                            new AssignVariable("a"),
                        }
                    },
                    new PrintVariable("a"),
                }
            },
            new FunctionDeclaration("Foo2")
            {
                Body =
                {
                    new Invocation("Bar", false),
                    new VariableDeclaration("a"),
                    new FunctionDeclaration("Bar")
                    {
                        Body =
                        {
                            new PrintVariable("a")
                        }
                    },
                    new PrintVariable("a"),
                }
            },
            new Invocation("Foo", false),
            new Invocation("Foo2", false),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
    Bar();
    var a;
    func Bar {{
        a = smth;
    }}

    print(a);
}}

func Foo2 {{
    Bar(); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
    var a;
    func Bar {{
        print(a);
    }}

    print(a);
}}

Foo();
Foo2();
");
    }

    [Test]
    public void Analyze_SameNameInNestedContextAfterDeclaration_AssignmentsAreDifferent()
    {
        var program = new Program
        {
            new FunctionDeclaration("Foo")
            {
                Body =
                {
                    new VariableDeclaration("a"),
                    new AssignVariable("a"),
                    new PrintVariable("a"),
                }
            },
            new VariableDeclaration("a"),
            new Invocation("Foo", false),
            new PrintVariable("a"),
        };

        ValidationHelper.ValidateResult(program, $@"
func Foo {{
    var a;
    a = smth;
    print(a);
}}

var a;
Foo();
print(a); {ValidationHelper.Error(s => new UnassignedVariableUsageDescriptor(s, "a"))}
");
    }

    [Test]
    public void Analyze_InternalCallUnconditionalButExternalCallIsConditional_AllVariableAccessesAreAnalyzed()
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
                    new Invocation("Bar", false),
                    new PrintVariable("b"),
                }
            },
            new FunctionDeclaration("Bar")
            {
                Body =
                {
                    new AssignVariable("b"),
                }
            },
            new Invocation("Foo", true),
        };

        ValidationHelper.ValidateResult(program, $@"
var a;
var b;
func Foo {{
    print(a);
    Bar();
    print(b);
}}

func Bar {{
    b = smth;
}}

if (smth) Foo(); {ValidationHelper.Errors(s => new UnassignedVariableUsageDescriptor[] {new(s, "a")})}
");
    }
}