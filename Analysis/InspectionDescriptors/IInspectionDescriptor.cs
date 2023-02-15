using LanguageModel;

namespace Analysis.InspectionDescriptors;

public interface IInspectionDescriptor
{
    IStatement ErrorStatement { get; }
}