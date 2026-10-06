using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PixelGameStudio.Domain.Profiles;

namespace PixelGameStudio.App.Views;

/// <summary>
/// Modal form collecting project identity before anything is created: name,
/// game profile (genre), perspective and canvas size. Environment setup (rig,
/// base character, presets) runs afterwards, once a save folder is chosen.
/// </summary>
public partial class NewProjectDialog : Window
{
    private static readonly Dictionary<string, string> PerspectiveLabels = new(StringComparer.Ordinal)
    {
        [KnownPerspectives.TopDown4] = "Top-down 4 hướng",
        [KnownPerspectives.TopDown8] = "Top-down 8 hướng",
        [KnownPerspectives.Diagonal4] = "Chéo 4 hướng",
        [KnownPerspectives.SideView2] = "Đi ngang 2 hướng",
        [KnownPerspectives.Frontal] = "Đối diện (front)",
        [KnownPerspectives.Custom] = "Tùy chỉnh",
    };

    private static readonly Dictionary<string, (int Width, int Height)> CanvasDefaults = new(StringComparer.Ordinal)
    {
        [KnownPerspectives.TopDown4] = (32, 46),
        [KnownPerspectives.TopDown8] = (64, 64),
        [KnownPerspectives.Diagonal4] = (32, 46),
        [KnownPerspectives.SideView2] = (48, 32),
        [KnownPerspectives.Frontal] = (32, 46),
        [KnownPerspectives.Custom] = (32, 46),
    };

    public NewProjectDialog()
    {
        InitializeComponent();
        GenreBox.ItemsSource = GameGenres.All;
        GenreBox.SelectedIndex = 0;
        PerspectiveBox.ItemsSource = KnownPerspectives.All
            .Select(id => new PerspectiveOption(id, PerspectiveLabels.GetValueOrDefault(id, id)))
            .ToList();
        PerspectiveBox.SelectedIndex = 0;
        UpdateDirectionHint();
    }

    public string ProjectName => NameBox.Text?.Trim() ?? string.Empty;

    public string SelectedGenre => (GenreBox.SelectedItem as string) ?? GameGenres.TopDownRpg;

    public string SelectedPerspective => (PerspectiveBox.SelectedItem as PerspectiveOption)?.Id ?? KnownPerspectives.TopDown4;

    public int CanvasWidth => int.TryParse(WidthBox.Text, out int w) ? Math.Clamp(w, 8, 1024) : 32;

    public int CanvasHeight => int.TryParse(HeightBox.Text, out int h) ? Math.Clamp(h, 8, 1024) : 46;

    private void OnPerspectiveChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (WidthBox is null || HeightBox is null)
        {
            return;
        }

        (int width, int height) = CanvasDefaults.GetValueOrDefault(SelectedPerspective, (32, 46));
        WidthBox.Text = width.ToString();
        HeightBox.Text = height.ToString();
        UpdateDirectionHint();
    }

    private void UpdateDirectionHint()
    {
        if (DirectionHint is null)
        {
            return;
        }

        IReadOnlyList<string> directions = SelectedPerspective switch
        {
            KnownPerspectives.TopDown8 => ViewProfile.TopDown8().Directions,
            KnownPerspectives.Diagonal4 => ViewProfile.Diagonal4().Directions,
            KnownPerspectives.SideView2 => ViewProfile.SideView2().Directions,
            KnownPerspectives.Frontal => ViewProfile.Frontal().Directions,
            _ => ViewProfile.TopDown4().Directions,
        };
        DirectionHint.Text = $"{directions.Count} hướng sprite: {string.Join(", ", directions)}";
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            ErrorText.Text = "Tên project không được để trống.";
            NameBox.Focus();
            return;
        }

        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    public sealed record PerspectiveOption(string Id, string Label);
}
