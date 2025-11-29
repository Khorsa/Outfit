using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool.ModulesAdapter;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    CommandContextMenu? ContextMenu { get; }
    void Run();
}