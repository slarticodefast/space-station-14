using Robust.Shared.GameStates;

namespace Content.Shared.Hands.Components;

/// <summary>
/// An entity with this component will give you extra hands when you quip it in your inventory.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExtraHandsEquipmentComponent : Component
{
    /// <summary>
    /// The number of hands to be added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int HandAmount = 2;

    /// <summary>
    /// The names of the currently added hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> HandNames = new();
}
