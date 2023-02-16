using System.Diagnostics;
using LanguageModel;

namespace Analysis;

public sealed class ParentAssignmentAnalyzer : IPostorderMethodStateAnalyzer<ParentAssignmentsContext>
{
    public ParentAssignmentsContext CreateEmptyContext(IProgramDeclarations declarations) => new(declarations);

    public void AnalyzeAssignVariable(ParentAssignmentsContext context, AssignVariable statement)
    {
        AnaliseVariableAssignment(context, statement.VariableName);
    }

    public void AnalyzePrintVariable(ParentAssignmentsContext context, PrintVariable statement)
    {
    }

    public void AnalyzeInvocation(ParentAssignmentsContext context, Invocation invocation,
        ParentAssignmentsContext invokedMethodContext, IInvokedMethodContextProvider contextProvider)
    {
        Debug.Assert(!invocation.IsConditional, "Conditional invocations should not be processed");

        foreach (var assignment in invokedMethodContext.ParentContextDefiniteAssignments)
        {
            //TODO can reduce memory footprint using ImmutableHashSet
            AnaliseVariableAssignment(context, assignment);
        }
    }

    public bool NeedToProcessInvocation(Invocation invocation) => !invocation.IsConditional;
    
    private static void AnaliseVariableAssignment(ParentAssignmentsContext context, string variableName)
    {
        if (context.CurrentContextVariableDeclarations.Contains(variableName))
        {
            return;
        }

        context.ParentContextDefiniteAssignments.Add(variableName);
    }
}

public sealed class ParentAssignmentsContext
{
    public ParentAssignmentsContext(IProgramDeclarations declarations)
    {
        CurrentContextVariableDeclarations = declarations.CurrentContextVariables;
    }

    public IReadOnlySet<string> CurrentContextVariableDeclarations { get; }
    public HashSet<string> ParentContextDefiniteAssignments { get; } = new();
}