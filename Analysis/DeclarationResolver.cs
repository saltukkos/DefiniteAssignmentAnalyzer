using System.Collections.Immutable;
using Analysis.InspectionDescriptors;
using LanguageModel;

namespace Analysis;

public sealed class DeclarationResolver
{
    private readonly IInspectionDescriptorCollector _inspectionDescriptorCollector;

    public DeclarationResolver(IInspectionDescriptorCollector inspectionDescriptorCollector)
    {
        _inspectionDescriptorCollector = inspectionDescriptorCollector;
    }

    public IDeclarationScope Resolve(Program rootProgram)
    {
        var programsToVisit = new Queue<DeclarationScope>();
        var rootDeclarations =
            new DeclarationScope(rootProgram, null, ImmutableDictionary<string, VariableDeclaration>.Empty);
        programsToVisit.Enqueue(rootDeclarations);

        while (programsToVisit.TryDequeue(out var currentScope))
        {
            AnalyzeSubProgram(currentScope, programsToVisit);
        }

        return rootDeclarations;
    }

    private void AnalyzeSubProgram(DeclarationScope currentScope, Queue<DeclarationScope> programsToVisit)
    {
        var parentFunctionDeclarations = currentScope.ParentScope?.AllAvailableFunctionDeclarationsInternal ??
                                 ImmutableDictionary<string, IDeclarationScope>.Empty;

        var availableFunctionDeclarationsBuilder = parentFunctionDeclarations.ToBuilder();
        var availableVariablesDeclarationsBuilder = currentScope.AllAvailableVariableDeclarationsInternal.ToBuilder();
        foreach (var statement in currentScope.Program)
        {
            if (statement is VariableDeclaration variableDeclaration)
            {
                currentScope.CurrentContextVariablesInternal.Add(variableDeclaration);
                var variableName = variableDeclaration.VariableName;
                if (availableVariablesDeclarationsBuilder.ContainsKey(variableName))
                {
                    _inspectionDescriptorCollector.ReportInspection(
                        new ConflictingIdentifierNameDescriptor(variableDeclaration, variableName));
                }
                else
                {
                    availableVariablesDeclarationsBuilder.Add(variableName, variableDeclaration);   
                }

                continue;
            }
            
            if (statement is not FunctionDeclaration functionDeclaration)
            {
                continue;
            }

            var functionName = functionDeclaration.FunctionName;
            if (availableFunctionDeclarationsBuilder.ContainsKey(functionName))
            {
                _inspectionDescriptorCollector.ReportInspection(
                    new ConflictingIdentifierNameDescriptor(functionDeclaration, functionDeclaration.FunctionName));

                continue;
            }

            var childDeclarations = new DeclarationScope(functionDeclaration.Body, currentScope,
                availableVariablesDeclarationsBuilder.ToImmutable());

            availableFunctionDeclarationsBuilder.Add(functionName, childDeclarations);
            programsToVisit.Enqueue(childDeclarations);
        }

        currentScope.AllAvailableFunctionDeclarationsInternal = availableFunctionDeclarationsBuilder.ToImmutable();
        currentScope.AllAvailableVariableDeclarationsInternal = availableVariablesDeclarationsBuilder.ToImmutable();
    }
    
    private sealed class DeclarationScope : IDeclarationScope
    {
        public DeclarationScope(Program program, DeclarationScope? parentScope,
            ImmutableDictionary<string, VariableDeclaration> allVariableDeclarations)
        {
            Program = program;
            ParentScope = parentScope;
            AllAvailableVariableDeclarationsInternal = allVariableDeclarations;
            AllAvailableFunctionDeclarationsInternal = ImmutableDictionary<string, IDeclarationScope>.Empty;
        }

        public Program Program { get; }
        public DeclarationScope? ParentScope { get; }

        internal ImmutableDictionary<string, IDeclarationScope> AllAvailableFunctionDeclarationsInternal { get; set; }
        internal ImmutableDictionary<string, VariableDeclaration> AllAvailableVariableDeclarationsInternal { get; set; }
        internal HashSet<VariableDeclaration> CurrentContextVariablesInternal { get; } = new();

        public IReadOnlyDictionary<string, IDeclarationScope> AllAvailableFunctionDeclarations =>
            AllAvailableFunctionDeclarationsInternal;

        public IReadOnlyDictionary<string, VariableDeclaration> AllAvailableVariableDeclarations =>
            AllAvailableVariableDeclarationsInternal;

        public IReadOnlySet<VariableDeclaration> CurrentContextVariableDeclarations => CurrentContextVariablesInternal;
    }
}