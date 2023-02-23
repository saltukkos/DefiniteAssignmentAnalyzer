using LanguageModel;

namespace Analysis.InspectionDescriptors;

public sealed record UnreachableStatementDescriptor(IStatement ErrorStatement)
    : IInspectionDescriptor
{
    public override string ToString() => "Statement is unreachable";
}