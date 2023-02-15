using LanguageModel;

namespace Analysis.InspectionDescriptors;

public sealed record UnknownVariableDescriptor(IStatement ErrorStatement, string IdentifierName)
    : IInspectionDescriptor
{
    public override string ToString() => $"Unknown function '{IdentifierName}'";
}