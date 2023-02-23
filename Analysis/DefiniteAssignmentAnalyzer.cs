using Analysis.InspectionDescriptors;
using LanguageModel;

namespace Analysis;

public sealed class DefiniteAssignmentAnalyzer : IPostorderMethodStateAnalyzer<DefiniteAssignmentContext>
{
    private readonly IProgramAnalyzer<ParentAssignmentsContext> _assignmentsAnalyzer;
    private readonly IInspectionDescriptorCollector _inspectionDescriptorCollector;

    public DefiniteAssignmentAnalyzer(
        IProgramAnalyzer<ParentAssignmentsContext> assignmentsAnalyzer,
        IInspectionDescriptorCollector inspectionDescriptorCollector)
    {
        _assignmentsAnalyzer = assignmentsAnalyzer;
        _inspectionDescriptorCollector = inspectionDescriptorCollector;
    }

    public DefiniteAssignmentContext CreateEmptyContext(IDeclarationScope declarations) => new(declarations);

    public DefiniteAssignmentContext CreateRecursionContext(IDeclarationScope declarations) => new(declarations);

    public void AnalyzeAssignVariable(DefiniteAssignmentContext context, AssignVariable statement)
    {
        if (!VerifyStatementIsReachable(context, statement))
        {
            return;
        }

        if (context.AllAvailableVariableDeclarations.TryGetValue(statement.VariableName, out var declaration))
        {
            context.CurrentContextAssignments.Add(declaration);
        }
    }

    public void AnalyzePrintVariable(DefiniteAssignmentContext context, PrintVariable statement)
    {
        if (!VerifyStatementIsReachable(context, statement))
        {
            return;
        }

        if (context.AllAvailableVariableDeclarations.TryGetValue(statement.VariableName, out var declaration))
        {
            AnalyzeVariableAccess(context, declaration, statement);
        }
    }

    public bool NeedToProcessInvocation(Invocation invocation) => true;

    public void AnalyzeInvocation(DefiniteAssignmentContext context, Invocation invocation,
        DefiniteAssignmentContext invokedMethodContext, IInvokedMethodContextProvider contextProvider)
    {
        if (!VerifyStatementIsReachable(context, invocation))
        {
            return;
        }

        foreach (var dependency in invokedMethodContext.ParentContextAssignDependencies)
        {
            AnalyzeVariableAccess(context, dependency, invocation);
        }

        if (!invocation.IsConditional)
        {
            var assignmentsContext = contextProvider.GetContext(_assignmentsAnalyzer);
            foreach (var assignment in assignmentsContext.ParentContextDefiniteAssignments)
            {
                context.CurrentContextAssignments.Add(assignment);
            }

            if (assignmentsContext.IsAlwaysRecursive)
            {
                context.SawInfiniteRecursion = true;
            }
        }
    }

    private void AnalyzeVariableAccess(
        DefiniteAssignmentContext context, VariableDeclaration variable, IStatement statement)
    {
        // We will consider all accessed variables as being assigned after the statement to avoid producing
        // duplicate inspections
        if (!context.CurrentContextAssignments.Add(variable))
        {
            return;
        }

        if (context.CurrentContextVariableDeclarations.Contains(variable))
        {
            _inspectionDescriptorCollector.ReportInspection(
                new UnassignedVariableUsageDescriptor(statement, variable.VariableName));
        }
        else
        {
            context.ParentContextAssignDependencies.Add(variable);
        }
    }

    private bool VerifyStatementIsReachable(DefiniteAssignmentContext context, IStatement statement)
    {
        if (context.SawInfiniteRecursion)
        {
            _inspectionDescriptorCollector.ReportInspection(new UnreachableStatementDescriptor(statement));
            return false;
        }

        return true;
    }
}

public sealed class DefiniteAssignmentContext
{
    public DefiniteAssignmentContext(IDeclarationScope declarationScope)
    {
        CurrentContextVariableDeclarations = declarationScope.CurrentContextVariableDeclarations;
        AllAvailableVariableDeclarations = declarationScope.AllAvailableVariableDeclarations;
    }

    public bool SawInfiniteRecursion { get; set; }
    public IReadOnlyDictionary<string,VariableDeclaration> AllAvailableVariableDeclarations { get; }
    public IReadOnlySet<VariableDeclaration> CurrentContextVariableDeclarations { get; }
    public HashSet<VariableDeclaration> CurrentContextAssignments { get; } = new();
    public HashSet<VariableDeclaration> ParentContextAssignDependencies { get; } = new();
}