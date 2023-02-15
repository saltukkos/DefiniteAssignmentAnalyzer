using Analysis.InspectionDescriptors;
using LanguageModel;

namespace Analysis;

public sealed class DefiniteAssignmentAnalyzer : IPostorderMethodStateAnalyzer<DefiniteAssignmentContext>
{
    private readonly IPostorderMethodStateAnalyzer<ParentAssignmentsContext> _assignmentsAnalyzer;
    private readonly IInspectionDescriptorCollector _inspectionDescriptorCollector;

    public DefiniteAssignmentAnalyzer(
        IPostorderMethodStateAnalyzer<ParentAssignmentsContext> assignmentsAnalyzer,
        IInspectionDescriptorCollector inspectionDescriptorCollector)
    {
        _assignmentsAnalyzer = assignmentsAnalyzer;
        _inspectionDescriptorCollector = inspectionDescriptorCollector;
    }

    public DefiniteAssignmentContext CreateEmptyContext() => new();

    public void AnalyzeAssignVariable(DefiniteAssignmentContext context, AssignVariable statement)
    {
        context.CurrentContextAssignments.Add(statement.VariableName);
    }

    public void AnalyzePrintVariable(DefiniteAssignmentContext context, PrintVariable statement)
    {
        AnalyzeVariableAccess(context, statement.VariableName, statement);
    }

    public void AnalyzeInvocation(DefiniteAssignmentContext context, Invocation invocation,
        DefiniteAssignmentContext invokedMethodContext, IInvokedMethodContextProvider contextProvider)
    {
        foreach (var dependency in invokedMethodContext.ParentContextAssignDependencies)
        {
            AnalyzeVariableAccess(context, dependency, invocation);
        }

        if (!invocation.IsConditional)
        {
            var assignments = contextProvider.GetContext(_assignmentsAnalyzer);
            foreach (var assignment in assignments.ParentContextDefiniteAssignments)
            {
                context.CurrentContextAssignments.Add(assignment);
            }
        }
    }

    private void AnalyzeVariableAccess(DefiniteAssignmentContext context, string variableName, IStatement statement)
    {
        if (context.CurrentContextAssignments.Contains(variableName))
        {
            return;
        }

        if (context.CurrentContextVariableDeclarations.Contains(variableName))
        {
            _inspectionDescriptorCollector.ReportInspection(
                new UnassignedVariableUsageDescriptor(statement, variableName));
        }
        else
        {
            context.ParentContextAssignDependencies.Add(variableName);
        }
    }
}

public sealed class DefiniteAssignmentContext
{
    public IReadOnlySet<string> CurrentContextVariableDeclarations { get; }
    public HashSet<string> CurrentContextAssignments { get; } = new();
    public HashSet<string> ParentContextAssignDependencies { get; } = new();
}