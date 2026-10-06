using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PixelGameStudio.App.Rendering;
using PixelGameStudio.Rendering;

namespace PixelGameStudio.App.ViewModels;

/// <summary>One row in the asset browser: thumbnail + labels, loaded from the disk thumbnail cache.</summary>
public partial class AssetItemViewModel : ObservableObject
{
    public AssetItemViewModel(string id, string displayName, string meta, string thumbnailPath)
    {
        Id = id;
        DisplayName = displayName;
        Meta = meta;
        try
        {
            Thumbnail = PngCodec.Decode(File.OpenRead(thumbnailPath)).ToWriteableBitmap();
        }
        catch
        {
            Thumbnail = null; // missing/corrupt thumbnail must not break the browser
        }
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Meta { get; }

    [ObservableProperty]
    private WriteableBitmap? _thumbnail;
}
