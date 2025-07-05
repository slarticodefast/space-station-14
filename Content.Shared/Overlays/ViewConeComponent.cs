using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Restricts the entity's vision to a view cone.
/// When added to a clothing item it will also grant the wearer the same overlay.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ViewConeComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle ViewArc = Angle.FromDegrees(90);

    [DataField, AutoNetworkedField]
    public float Alpha = 0.2f;

    [DataField, AutoNetworkedField]
    public float CircleRadius = 1.0f;
}
