using Analysis.InspectionDescriptors;
using LanguageModel;

namespace Analysis;

public sealed class InvalidDeclarationsAnalyzer : IPreorderDeclarationsAnalyzer<DeclarationsContext>
{
    private readonly IInspectionDescriptorCollector _inspectionDescriptorCollector;

    public InvalidDeclarationsAnalyzer(IInspectionDescriptorCollector inspectionDescriptorCollector)
    {
        _inspectionDescriptorCollector = inspectionDescriptorCollector;
    }
    
    public DeclarationsContext CreateEmptyContext(IDeclarationScope declarations) => new(declarations);

    public DeclarationsContext CreateChildContext(DeclarationsContext context, IDeclarationScope nestedDeclarations)
    {
        return new DeclarationsContext(nestedDeclarations);
    }

    public void AnalyzeVariableDeclaration(DeclarationsContext context, VariableDeclaration declaration)
    {
        var variableName = declaration.VariableName;
        if (context.AllAvailableFunctionDeclarations.ContainsKey(variableName))
        {
            _inspectionDescriptorCollector
                .ReportInspection(new ConflictingIdentifierNameDescriptor(declaration, variableName));
        }
    }

    public void AnalyzeAssignVariable(DeclarationsContext context, AssignVariable statement)
    {
        AnalyzeVariableAccess(context, statement, statement.VariableName);
    }

    public void AnalyzePrintVariable(DeclarationsContext context, PrintVariable statement)
    {
        AnalyzeVariableAccess(context, statement, statement.VariableName);
    }

    public void AnalyzeInvocation(DeclarationsContext context, Invocation invocation)
    {
        if (!context.AllAvailableFunctionDeclarations.ContainsKey(invocation.FunctionName))
        {
            _inspectionDescriptorCollector
                .ReportInspection(new UnknownFunctionDescriptor(invocation, invocation.FunctionName));
        }
    }

    private void AnalyzeVariableAccess(DeclarationsContext context, IStatement statement, string variableName)
    {
        if (!context.AllAvailableVariableDeclarations.ContainsKey(variableName))
        {
            _inspectionDescriptorCollector
                .ReportInspection(new UnknownVariableDescriptor(statement, variableName));
        }
    }
}

public sealed class DeclarationsContext
{
    public DeclarationsContext(IDeclarationScope declarations)
    {
        AllAvailableFunctionDeclarations = declarations.AllAvailableFunctionDeclarations;
        AllAvailableVariableDeclarations = declarations.AllAvailableVariableDeclarations;
    }

    public IReadOnlyDictionary<string,VariableDeclaration> AllAvailableVariableDeclarations { get; }

    public IReadOnlyDictionary<string,IDeclarationScope> AllAvailableFunctionDeclarations { get; }
}