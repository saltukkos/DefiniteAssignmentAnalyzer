using LanguageModel;

namespace Analysis.InspectionDescriptors;

public sealed record UnassignedVariableUsageDescriptor(IStatement ErrorStatement, string VariableName)
    : IInspectionDescriptor
{
    public override string ToString() =>
        $"Variable may not have been initialized before accessing: '{VariableName}'";
}