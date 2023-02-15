using LanguageModel;

namespace Analysis.InspectionDescriptors;

public sealed record ConflictingIdentifierNameDescriptor(IStatement ErrorStatement, string IdentifierName)
    : IInspectionDescriptor
{
    public override string ToString() => $"Identifier with name '{IdentifierName}' is already declared";
}