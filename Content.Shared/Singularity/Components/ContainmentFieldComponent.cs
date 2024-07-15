using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainmentFieldComponent : Component
{

    /// <summary>
    /// How far an entity gets thrown after collision
    /// </summary>
    [DataField]
    public float ThrowDistance = 1f;

    /// <summary>
    /// The throw speed an entity has after collision
    /// </summary>
    [DataField]
    public float ThrowSpeed = 100f;

    /// <summary>
    /// This shouldn't be at 99999 or higher to prevent the singulo glitching out
    /// Will throw anything at the supplied mass or less that collides with the field.
    /// </summary>
    [DataField]
    public float MaxMass = 10000f;

    /// <summary>
    /// Should field vaporize garbage that collides with it?
    /// </summary>
    [DataField]
    public bool DestroyGarbage = true;
}
