using System.Diagnostics;
using LanguageModel;

namespace Analysis;

public sealed class ParentAssignmentAnalyzer : IPostorderMethodStateAnalyzer<ParentAssignmentsContext>
{
    public ParentAssignmentsContext CreateEmptyContext(IDeclarationScope declarations) => new(declarations, false);

    public ParentAssignmentsContext CreateRecursionContext(IDeclarationScope declarations) => new(declarations, true);

    public void AnalyzeAssignVariable(ParentAssignmentsContext context, AssignVariable statement)
    {
        if (context.AllAvailableVariableDeclarations.TryGetValue(statement.VariableName, out var declaration))
        {
            context.ParentContextDefiniteAssignments.Add(declaration);
        }
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
            context.ParentContextDefiniteAssignments.Add(assignment);
        }
        
        if (invokedMethodContext.IsAlwaysRecursive)
        {
            context.IsAlwaysRecursive = true;
        }
    }
}

public sealed class ParentAssignmentsContext
{
    public ParentAssignmentsContext(IDeclarationScope declarations, bool isAlwaysRecursive)
    {
        IsAlwaysRecursive = isAlwaysRecursive;
        AllAvailableVariableDeclarations = declarations.AllAvailableVariableDeclarations;
    }

    public bool IsAlwaysRecursive { get; set; }

    public IReadOnlyDictionary<string, VariableDeclaration> AllAvailableVariableDeclarations { get; }
    
    /*
     * Unfortunately, ImmutableHashSet will be not so efficient in this scenario because we have to be able
     * to merge data from multiple child contexts into one, but the Union operantion can't reuse internal structure
     * of both sets.
     * So we will not benefit as much in terms of memory footprint, and a regular HashSet would perform better here.
    */
    public HashSet<VariableDeclaration> ParentContextDefiniteAssignments { get; } = new();
}