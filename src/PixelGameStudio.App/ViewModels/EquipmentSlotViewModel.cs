using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;

namespace PixelGameStudio.App.ViewModels;

/// <summary>One equipment slot row in the character inspector: data-driven options from the asset library.</summary>
public partial class EquipmentSlotViewModel : ObservableObject
{
    public const string NoneOption = "— Không —";

    public EquipmentSlotViewModel(string slotId, string displayName, IReadOnlyList<string> options)
    {
        SlotId = slotId;
        DisplayName = displayName;
        Options = new ObservableCollection<string>(options);
        OptionItems = new ObservableCollection<EquipmentOptionViewModel>(
            options.Select(id => new EquipmentOptionViewModel(id, id, null, "")));
        AssignSelectedCommand = new RelayCommand(() => OnAssignSelected?.Invoke(SlotId));
    }

    public string SlotId { get; }

    public string DisplayName { get; }

    public ObservableCollection<string> Options { get; }

    /// <summary>Same choices with display metadata for the thumbnail selector.</summary>
    public ObservableCollection<EquipmentOptionViewModel> OptionItems { get; }

    public IRelayCommand AssignSelectedCommand { get; }

    /// <summary>Set by MainViewModel to bridge the contextual asset browser.</summary>
    public Action<string>? OnAssignSelected { get; set; }

    [ObservableProperty]
    private string? _selectedOption;

    /// <summary>Set by the owner VM; args: (slotId, selectedAssetIdOrNull).</summary>
    public Action<string, string?>? OnChanged { get; set; }

    partial void OnSelectedOptionChanged(string? value)
    {
        if (value is not null)
        {
            OnChanged?.Invoke(SlotId, value == NoneOption ? null : value);
        }
    }
}

public sealed class EquipmentOptionViewModel
{
    public EquipmentOptionViewModel(string id, string displayName, WriteableBitmap? thumbnail, string meta)
    {
        Id = id;
        DisplayName = displayName;
        Thumbnail = thumbnail;
        Meta = meta;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public WriteableBitmap? Thumbnail { get; }
    public string Meta { get; }
}
