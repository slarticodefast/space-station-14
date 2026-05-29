using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DoAfter;

/// <summary>
/// Component that is added to a DoAfter entity.
/// This stores all relevant information about a single DoAfter instance.
/// The entity is spawned when the DoAfter is started.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDoAfterSystem))]
public sealed partial class DoAfterEntityComponent : Component
{
    /// <summary>
    /// The parameters for the DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DoAfterArgs Args = default!;

    /// <summary>
    /// The entity performing the DoAfter.
    /// This should always be the same as the parent of the DoAfter entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid User;

    /// <summary>
    /// Target of the DoAfter (if relevant).
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// The entity used to perform the DoAfter.
    /// For example a tool.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Used;

    /// <summary>
    /// Entity which will receive the directed event. If null, no directed event will be raised.
    /// </summary>
    [DataField]
    public EntityUid? EventTarget;

    /// <summary>
    /// Time at which this do after was started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer), required: true)]
    public TimeSpan StartTime;

    /// <summary>
    /// The time at which this do after was canceled.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer), required: true)]
    public TimeSpan? CancelledTime;

    /// <summary>
    /// If true, this do after has finished, passed the final checks, and has raised its events.
    /// </summary>
    [DataField]
    public bool Completed;

    /// <summary>
    /// Whether the do after has been canceled.
    /// </summary>
    [ViewVariables]
    public bool Cancelled => CancelledTime != null;

    /// <summary>
    /// Position of the user relative to their parent when the do after was started.
    /// </summary>
    [DataField]
    public EntityCoordinates UserPosition;

    /// <summary>
    /// Distance from the user to the target when the do after was started.
    /// </summary>
    [DataField]
    public float TargetDistance;

    /// <summary>
    /// If <see cref="DoAfterArgs.NeedHand"/> is true, this is the hand that was selected when the doafter started.
    /// </summary>
    [DataField]
    public string? InitialHand;

    /// <summary>
    /// If <see cref="NeedHand"/> is true, this is the entity that was in the active hand when the doafter started.
    /// </summary>
    [DataField]
    public EntityUid? InitialItem;

    /// <summary>
    /// Cached attempt event for the sake of avoiding unnecessary reflection every time this needs to be raised.
    /// </summary>
    [ViewVariables]
    public object? AttemptEvent;
}
