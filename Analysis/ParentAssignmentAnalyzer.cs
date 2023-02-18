using System.Diagnostics;
using LanguageModel;

namespace Analysis;

public sealed class ParentAssignmentAnalyzer : IPostorderMethodStateAnalyzer<ParentAssignmentsContext>
{
    public ParentAssignmentsContext CreateEmptyContext(IDeclarationScope declarations) => new(declarations);

    public void AnalyzeAssignVariable(ParentAssignmentsContext context, AssignVariable statement)
    {
        AnaliseVariableAssignment(context, statement.VariableName);
    }

    public void AnalyzePrintVariable(ParentAssignmentsContext context, PrintVariable statement)
    {
    }

    public bool NeedToProcessInvocation(Invocation invocation) => !invocation.IsConditional;

    public void AnalyzeInvocation(ParentAssignmentsContext context, Invocation invocation,
        ParentAssignmentsContext invokedMethodContext, IInvokedMethodContextProvider contextProvider)
    {
        Debug.Assert(!invocation.IsConditional, "Conditional invocations should not be processed");

        foreach (var assignment in invokedMethodContext.ParentContextDefiniteAssignments)
        {
            AnaliseVariableAssignment(context, assignment);
        }
    }

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
    public ParentAssignmentsContext(IDeclarationScope declarations)
    {
        CurrentContextVariableDeclarations = declarations.CurrentContextVariables;
    }

    public IReadOnlySet<string> CurrentContextVariableDeclarations { get; }
    
    /*
     * Unfortunately, ImmutableHashSet will be not so efficient in this scenario because we have to be able
     * to merge data from multiple child contexts into one, but the Union operantion can't reuse internal structure
     * of both sets.
     * So we will not benefit as much in terms of memory footprint, and a regular HashSet would perform better here.
    */
    public HashSet<string> ParentContextDefiniteAssignments { get; } = new();
}