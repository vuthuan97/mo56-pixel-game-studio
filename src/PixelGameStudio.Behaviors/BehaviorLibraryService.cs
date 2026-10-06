using System.Text.Json;
using System.Text.Json.Serialization;
using PixelGameStudio.Domain;
using PixelGameStudio.Domain.Animation;
using PixelGameStudio.Domain.Behaviors;
using PixelGameStudio.Domain.Templates;

namespace PixelGameStudio.Behaviors;

/// <summary>
/// Behavior library service: installs preset groups as data, creates/duplicates
/// custom behaviors and exports/imports behavior templates as JSON files.
/// Presets are never hard-coded into the UI — the UI reads project.Behaviors.
/// </summary>
public sealed class BehaviorLibraryService
{
    public static JsonSerializerOptions TemplateJsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Installs every preset behavior and pose/animation preset set missing from the project.</summary>
    public int InstallPresets(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        int added = 0;
        foreach (PoseDefinition pose in PosePresets.All)
        {
            if (project.Poses.All(p => p.Id != pose.Id))
            {
                project.Poses.Add(pose);
                added++;
            }
        }

        foreach (AnimationDefinition animation in AnimationPresets.All)
        {
            if (project.Animations.All(a => a.Id != animation.Id))
            {
                project.Animations.Add(animation);
                added++;
            }
        }

        foreach (BehaviorDefinition behavior in BehaviorPresets.All)
        {
            if (project.Behaviors.All(b => b.Id != behavior.Id))
            {
                project.Behaviors.Add(behavior);
                added++;
            }
        }

        return added;
    }

    /// <summary>Duplicates an existing behavior (preset or custom) into a new custom behavior.</summary>
    public BehaviorDefinition CreateCustom(Project project, string sourceBehaviorId, string newId, string newDisplayName)
    {
        ArgumentNullException.ThrowIfNull(project);
        BehaviorDefinition source = project.Behaviors.FirstOrDefault(b => b.Id == sourceBehaviorId)
            ?? throw new InvalidOperationException($"Không tìm thấy behavior nguồn '{sourceBehaviorId}'.");
        if (project.Behaviors.Any(b => b.Id == newId))
        {
            throw new InvalidOperationException($"Behavior id '{newId}' đã tồn tại.");
        }

        var copy = new BehaviorDefinition
        {
            Id = newId,
            DisplayName = newDisplayName,
            Group = source.Group,
            AnimationId = source.AnimationId,
            HeldItemSlotId = source.HeldItemSlotId,
            HeldItemAssetId = source.HeldItemAssetId,
            InteractionAnchorId = source.InteractionAnchorId,
            Markers = source.Markers.Select(m => new BehaviorMarker(m.Type, m.Name, m.FrameIndex)).ToList(),
            Notes = source.Notes,
        };
        project.Behaviors.Add(copy);
        return copy;
    }

    /// <summary>Deletes a behavior; preset behaviors are protected.</summary>
    public void Delete(Project project, string behaviorId)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (BehaviorPresets.IsPresetId(behaviorId))
        {
            throw new InvalidOperationException("Behavior preset không thể xóa — nhân bản thành behavior tùy chỉnh rồi sửa.");
        }

        project.Behaviors.RemoveAll(b => b.Id == behaviorId);
    }

    public IReadOnlyList<BehaviorDefinition> ListByGroup(Project project, string? group = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        IEnumerable<BehaviorDefinition> behaviors = project.Behaviors;
        if (!string.IsNullOrWhiteSpace(group))
        {
            behaviors = behaviors.Where(b => b.Group.Equals(group, StringComparison.OrdinalIgnoreCase));
        }

        return behaviors.OrderBy(b => b.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Writes one behavior as a reusable template JSON file.</summary>
    public void ExportTemplate(BehaviorDefinition behavior, string filePath)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        string json = JsonSerializer.Serialize(behavior, TemplateJsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>Reads a behavior template; the caller decides the id to install it under.</summary>
    public BehaviorDefinition ImportTemplate(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string json = File.ReadAllText(filePath);
        BehaviorDefinition? behavior;
        try
        {
            behavior = JsonSerializer.Deserialize<BehaviorDefinition>(json, TemplateJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"File behavior template không hợp lệ: {ex.Message}");
        }

        return behavior ?? throw new InvalidDataException("File behavior template rỗng.");
    }

    /// <summary>Installs an imported behavior under (possibly) a new id.</summary>
    public BehaviorDefinition Install(Project project, BehaviorDefinition behavior, string? newId = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(behavior);
        string id = newId ?? behavior.Id;
        if (project.Behaviors.Any(b => b.Id == id))
        {
            throw new InvalidOperationException($"Behavior id '{id}' đã tồn tại.");
        }

        var installed = new BehaviorDefinition
        {
            Id = id,
            DisplayName = behavior.DisplayName,
            Group = behavior.Group,
            AnimationId = behavior.AnimationId,
            HeldItemSlotId = behavior.HeldItemSlotId,
            HeldItemAssetId = behavior.HeldItemAssetId,
            InteractionAnchorId = behavior.InteractionAnchorId,
            Markers = behavior.Markers.Select(m => new BehaviorMarker(m.Type, m.Name, m.FrameIndex)).ToList(),
            Notes = behavior.Notes,
        };
        project.Behaviors.Add(installed);
        return installed;
    }
}
