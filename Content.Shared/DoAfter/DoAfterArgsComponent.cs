using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.DoAfter;

/// <summary>
/// For setting DoAfterArgs on an entity level.
/// When added to an action entity, that action will start a DoAfter with those settings before activating.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoAfterSystem))]
public sealed partial class DoAfterArgsComponent : Component
{
    /// <summary>
    /// A set of <see cref="DoAfterArgs"/>.
    /// The <see cref="ActionDoAfterEvent"/> is inserted when <see cref="SharedActionsSystem.TryStartActionDoAfter"/> is called.
    /// </summary>
    [DataField]
    public DoAfterArgs DoAfterArgs;

    /// <summary>
    /// Should this DoAfter repeat after being completed?
    /// </summary>
    [DataField]
    public bool Repeat;

    /// <summary>
    /// What should the delay be reduced to after completion?
    /// </summary>
    [DataField]
    public TimeSpan? DelayReduction;
}
