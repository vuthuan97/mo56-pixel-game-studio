using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

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
    }

    public string SlotId { get; }

    public string DisplayName { get; }

    public ObservableCollection<string> Options { get; }

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
