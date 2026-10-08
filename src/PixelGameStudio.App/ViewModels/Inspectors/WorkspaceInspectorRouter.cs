namespace PixelGameStudio.App.ViewModels.Inspectors;

/// <summary>
/// Resolves the inspector for the selected top-level workspace and character
/// section. Keeping this routing outside MainViewModel prevents the shell from
/// owning workspace-specific view composition rules.
/// </summary>
public sealed class WorkspaceInspectorRouter
{
    private readonly ProjectInspectorViewModel _project;
    private readonly CharacterInspectorViewModel _character;
    private readonly EquipmentInspectorViewModel _equipment;
    private readonly AnimationInspectorViewModel _animation;
    private readonly BehaviorInspectorViewModel _actions;
    private readonly ExportInspectorViewModel _export;
    private readonly WorkspacePlaceholderViewModel _library;
    private readonly WorkspacePlaceholderViewModel _background;

    public WorkspaceInspectorRouter(
        ProjectInspectorViewModel project,
        CharacterInspectorViewModel character,
        EquipmentInspectorViewModel equipment,
        AnimationInspectorViewModel animation,
        BehaviorInspectorViewModel actions,
        ExportInspectorViewModel export,
        WorkspacePlaceholderViewModel library,
        WorkspacePlaceholderViewModel background)
    {
        _project = project;
        _character = character;
        _equipment = equipment;
        _animation = animation;
        _actions = actions;
        _export = export;
        _library = library;
        _background = background;
    }

    public object? Resolve(string workspace, string section) => workspace switch
    {
        "Project" => _project,
        "Character" => section switch
        {
            "Equipment" => _equipment,
            "Animation" => _animation,
            "Actions" => _actions,
            _ => _character,
        },
        "Library" => _library,
        "Background" => _background,
        "Export" => _export,
        _ => null,
    };
}
