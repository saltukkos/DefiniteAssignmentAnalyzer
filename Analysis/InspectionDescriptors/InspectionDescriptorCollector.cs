using LanguageModel;

namespace Analysis.InspectionDescriptors;

public interface IInspectionDescriptorCollector
{
    void ReportInspection(IInspectionDescriptor inspectionDescriptor);
}

public sealed class InspectionDescriptorCollector
    : IInspectionDescriptorCollector
{
    private readonly Dictionary<IStatement, HashSet<IInspectionDescriptor>> _inspections = new();

    public void ReportInspection(IInspectionDescriptor inspectionDescriptor)
    {
        var location = inspectionDescriptor.ErrorStatement;
        if (_inspections.TryGetValue(location, out var inspections))
        {
            inspections.Add(inspectionDescriptor);
            return;
        }

        _inspections.Add(location, new HashSet<IInspectionDescriptor> {inspectionDescriptor});
    }
}