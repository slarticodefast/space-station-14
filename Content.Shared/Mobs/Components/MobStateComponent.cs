using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components;

/// <summary>
/// When attached to an <see cref="DamageableComponent"/>,
/// this component will handle critical and death behaviors for mobs.
/// Additionally, it handles sending effects to clients
/// (such as blur effect for unconsciousness) and managing the health HUD.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(MobStateSystem), typeof(MobThresholdSystem))]
public sealed partial class MobStateComponent : Component
{
    /// <summary>
    /// The current MobState this entity is in.
    /// </summary>
    /// <remarks>
    /// The default mobstate is always the lowest state level.
    /// </remarks>
    [DataField, ViewVariables]
    public MobState CurrentState = MobState.Alive;

    /// <summary>
    /// The MobStates allowed for this entity.
    /// </summary>
    [DataField]
    public HashSet<MobState> AllowedStates = new()
        {
            MobState.Alive,
            MobState.Critical,
            MobState.Dead
        };
}

/// <summary>
/// Manual networking state for <see cref="MobStateComponent"/>.
/// Needed so that we can raise events on the client when handling networked changes to the component.
/// <seealso cref="MobStateChangedEvent"/>
/// </summary>
[Serializable, NetSerializable]
public sealed class MobStateComponentState(MobState currentState, HashSet<MobState> allowedStates) : ComponentState
{
    public readonly MobState CurrentState = currentState;
    public readonly HashSet<MobState> AllowedStates = allowedStates;
}
