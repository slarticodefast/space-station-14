using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class ViewConeOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public Angle ViewArc = Angle.FromDegrees(90);
    public float Alpha = 0.2f;
    public float CircleRadius = 1.0f;

    private readonly ShaderInstance _viewConeShader;
    private readonly SharedTransformSystem _transform;

    public ViewConeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _viewConeShader = _prototypeManager.Index<ShaderPrototype>("ViewCone").InstanceUnique();
        _transform = _entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var player = _player.LocalEntity;
        if (player == null)
            return;

        // Get mouse loc and convert to angle based on player location
        var coords = _input.MouseScreenPosition;
        var mapPos = _eye.PixelToMap(coords);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        var angle = (mapPos.Position - _transform.GetMapCoordinates(player.Value).Position).ToWorldAngle();

        _entManager.TryGetComponent<EyeComponent>(player, out var eyeComp);
        var zoom = eyeComp?.Zoom.X ?? 1.0f;

        var handle = args.WorldHandle;
        _viewConeShader.SetParameter("viewArc", (float)ViewArc.Theta);
        _viewConeShader.SetParameter("alpha", Alpha);
        _viewConeShader.SetParameter("circleRadius", CircleRadius);
        _viewConeShader.SetParameter("angle", (float)angle.Theta);
        _viewConeShader.SetParameter("zoom", zoom);

        handle.UseShader(_viewConeShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
