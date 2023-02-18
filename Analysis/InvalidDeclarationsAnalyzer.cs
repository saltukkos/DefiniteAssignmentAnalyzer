using System.Collections.Immutable;
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
        return new DeclarationsContext(nestedDeclarations, context);
    }

    public void AnalyzeVariableDeclaration(DeclarationsContext context, VariableDeclaration declaration)
    {
        var variableName = declaration.VariableName;
        if (!context.DeclaredVariables.Add(variableName))
        {
            _inspectionDescriptorCollector
                .ReportInspection(new ConflictingIdentifierNameDescriptor(declaration, variableName));
            return;
        }

        /*
         * Handle this case after variable name conflicts, because we still want to save varible declaration
         * to produce less irrelevant inspections in case of variable\function name conflict.
        */
        if (context.Declarations.AllAvailableFunctionDeclarations.ContainsKey(variableName))
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
        if (!context.Declarations.AllAvailableFunctionDeclarations.ContainsKey(invocation.FunctionName))
        {
            _inspectionDescriptorCollector
                .ReportInspection(new UnknownFunctionDescriptor(invocation, invocation.FunctionName));
        }
    }

    private void AnalyzeVariableAccess(DeclarationsContext context, IStatement statement, string variableName)
    {
        if (!context.DeclaredVariables.Contains(variableName))
        {
            _inspectionDescriptorCollector
                .ReportInspection(new UnknownVariableDescriptor(statement, variableName));
        }
    }
}

public sealed class DeclarationsContext
{
    public DeclarationsContext(IDeclarationScope declarations, DeclarationsContext? parentContext = null)
    {
        Declarations = declarations;
        DeclaredVariables = parentContext?.DeclaredVariables.ToImmutable().ToBuilder() ??
                            ImmutableHashSet.CreateBuilder<string>();
    }

    public IDeclarationScope Declarations { get; }

    public ImmutableHashSet<string>.Builder DeclaredVariables { get; }
}