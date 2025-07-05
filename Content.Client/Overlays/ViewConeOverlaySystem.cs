using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class ViewConeSystem : EquipmentHudSystem<ViewConeComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private ViewConeOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ViewConeComponent> args)
    {
        base.UpdateInternal(args);

        var viewArc = Angle.FromDegrees(Math.Tau);
        var alpha = 1.0f;
        var circleRadius = float.PositiveInfinity;
        _overlayMan.AddOverlay(_overlay);

        // if we have multiple components restricting our vision, then choose the most restricting parameters
        foreach (var comp in args.Components)
        {
            if (comp.ViewArc < viewArc)
                viewArc = comp.ViewArc;
            if (comp.Alpha < alpha)
                alpha = comp.Alpha;
            if (comp.CircleRadius < circleRadius)
                circleRadius = comp.CircleRadius;
        }

        _overlay.ViewArc = viewArc;
        _overlay.Alpha = alpha;
        _overlay.CircleRadius = circleRadius;
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
