using PixelGameStudio.Domain;

namespace PixelGameStudio.ProjectSystem;

/// <summary>
/// Snapshot-based undo/redo for the open project (memento pattern). Every
/// checkpoint stores a full JSON snapshot — the project file is small text
/// metadata, so this is cheap and impossible to get out of sync with model
/// changes. Call <see cref="Checkpoint"/> BEFORE a mutation; <see cref="Undo"/>
/// then restores the pre-mutation state. The service never mutates project
/// data itself; Undo/Redo return a fresh deserialized Project to adopt.
/// </summary>
public sealed class UndoRedoService
{
    private const int MaxEntries = 50;

    private readonly ProjectStore _store;
    private readonly List<string> _undo = [];
    private readonly List<string> _redo = [];

    public UndoRedoService(ProjectStore store) => _store = store;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public int UndoDepth => _undo.Count;

    /// <summary>Resets history.</summary>
    public void Attach(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        _undo.Clear();
        _redo.Clear();
    }

    /// <summary>Call BEFORE a mutation: snapshots the current state onto the undo stack and clears redo.</summary>
    public void Checkpoint(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        _undo.Add(_store.Serialize(project));
        if (_undo.Count > MaxEntries)
        {
            _undo.RemoveRange(0, _undo.Count - MaxEntries);
        }

        _redo.Clear();
    }

    /// <summary>Restores the previous state; returns a fresh Project the caller must adopt.</summary>
    public Project? Undo(Project project)
    {
        if (!CanUndo)
        {
            return null;
        }

        string snapshot = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(_store.Serialize(project));
        return _store.Deserialize(snapshot);
    }

    /// <summary>Re-applies the last undone state; returns a fresh Project the caller must adopt.</summary>
    public Project? Redo(Project project)
    {
        if (!CanRedo)
        {
            return null;
        }

        string snapshot = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(_store.Serialize(project));
        return _store.Deserialize(snapshot);
    }
}
