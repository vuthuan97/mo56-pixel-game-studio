namespace PixelGameStudio.Rendering.Procedural;

/// <summary>
/// Procedural source used only when starter assets are generated. The result is
/// still exported as ordinary project assets and composed by the data-driven
/// rig, so changing this source never couples the editor to a renderer.
/// </summary>
public interface ICharacterLayerRenderer
{
    List<(string Name, PixelBuffer Image)> RenderLayers(LegacySpriteSpec spec, LegacyPose pose);
}
