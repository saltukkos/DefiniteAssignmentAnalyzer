using LanguageModel;

namespace Analysis.InspectionDescriptors;

public sealed record UnknownFunctionDescriptor(IStatement ErrorStatement, string IdentifierName)
    : IInspectionDescriptor
{
    public override string ToString() => $"Unknown function '{IdentifierName}'";
}