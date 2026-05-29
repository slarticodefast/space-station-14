using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

/// <summary>
/// This contains all the parameters for DoAfters.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DoAfterArgs
{
    /// <summary>
    /// The DoAfter entity to spawn.
    /// Change this if you want it to have additional components.
    /// </summary>
    [DataField]
    public EntProtoId<DoAfterEntityComponent> Prototype = "DefaultDoAfter";

    /// <summary>
    /// How long does the DoAfter require to complete.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Delay;

    /// <summary>
    /// Whether the progress bar for this DoAfter should be hidden from other players.
    /// </summary>
    [DataField]
    public bool Hidden;

    #region Event options
    /// <summary>
    /// The event that will get raised when the DoAfter has finished. If null, this will simply raise a <see cref="SimpleDoAfterEvent"/>.
    /// </summary>
    [DataField(required: true)]
    public DoAfterEvent Event = default!;

    /// <summary>
    /// This option determines how frequently the DoAfterAttempt event will get raised. Defaults to never raising the event.
    /// </summary>
    [DataField]
    public AttemptFrequency AttemptFrequency;

    /// <summary>
    /// Should the DoAfter event broadcast? If this is false, then <see cref="EventTarget"/> should be a valid entity.
    /// </summary>
    [DataField]
    public bool Broadcast;
    #endregion

    #region Break/Cancellation Options
    // Break the chains
    /// <summary>
    /// Whether or not this do after requires the user to have hands.
    /// </summary>
    [DataField]
    public bool NeedHand;

    /// <summary>
    /// Whether we need to keep our active hand as is (i.e. can't change hand or change item). This also covers
    /// requiring the hand to be free (if applicable). This does nothing if <see cref="NeedHand"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <summary>
    /// Whether the DoAfter should get interrupted if we drop the
    /// active item we started the DoAfter with.
    /// This does nothing if <see cref="NeedHand"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnDropItem = true;

    /// <summary>
    /// If the DoAfter stops when the user or target moves.
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    /// <summary>
    /// Whether to break on movement when the user is weightless.
    /// This does nothing if <see cref="BreakOnMove"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnWeightlessMove = true;

    /// <summary>
    /// Threshold for user and target movement.
    /// </summary>
    [DataField]
    public float MovementThreshold = 0.3f;

    /// <summary>
    /// Threshold for distance user from the used OR target entities.
    /// </summary>
    [DataField]
    public float? DistanceThreshold = 1.5f;

    /// <summary>
    /// Whether damage will cancel the DoAfter. See also <see cref="DamageThreshold"/>.
    /// </summary>
    [DataField]
    public bool BreakOnDamage;

    /// <summary>
    /// Threshold for user damage. This damage has to be dealt in a single event, not over time.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 1;

    /// <summary>
    /// If true, this DoAfter will be canceled if the user can no longer interact with the target.
    /// </summary>
    [DataField]
    public bool RequireCanInteract = true;
    #endregion

    #region Duplicates
    /// <summary>
    /// If true, this will prevent duplicate DoAfters from being started See also <see cref="DuplicateConditions"/>.
    /// </summary>
    /// <remarks>
    /// Note that this will block even if the duplicate is cancelled because either DoAfter had
    /// <see cref="CancelDuplicate"/> enabled.
    /// </remarks>
    [DataField]
    public bool BlockDuplicate = true;

    //TODO: User pref to not cancel on second use on specific doafters
    /// <summary>
    /// If true, this will cancel any duplicate DoAfters when attempting to add a new DoAfter. See also
    /// <see cref="DuplicateConditions"/>.
    /// </summary>
    [DataField]
    public bool CancelDuplicate = true;

    /// <summary>
    /// These flags determine what DoAfter properties are used to determine whether one DoAfter is a duplicate of another.
    /// </summary>
    /// <remarks>
    /// Note that both DoAfters may have their own conditions, and they will be considered duplicated if either set
    /// of conditions is satisfied.
    /// </remarks>
    [DataField]
    public DuplicateConditions DuplicateCondition = DuplicateConditions.All;
    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new set of DoAfter arguments.
    /// </summary>
    public DoAfterArgs()
    {
    }

    /// <summary>
    /// Creates a new set of DoAfter arguments.
    /// </summary>
    /// <param name="delay">The time it takes for the DoAfter to complete.</param>
    /// <param name="event">The event that will be raised when the DoAfter has ended (completed or cancelled).</param>
    public DoAfterArgs(
        TimeSpan delay,
        DoAfterEvent @event)
    {
        Delay = delay;
        Event = @event;
    }

    /// <summary>
    /// Creates a new set of DoAfter arguments.
    /// </summary>
    /// <param name="seconds">The time it takes for the DoAfter to complete, in seconds</param>
    /// <param name="event">The event that will be raised when the DoAfter has ended (completed or cancelled).</param>
    public DoAfterArgs(
        float seconds,
        DoAfterEvent @event)
        : this(TimeSpan.FromSeconds(seconds), @event)
    {
    }

    #endregion

    //The almighty pyramid returns.......
    public DoAfterArgs(DoAfterArgs other)
    {
        Prototype = other.Prototype;
        Delay = other.Delay;
        Hidden = other.Hidden;
        Event = other.Event.Clone();
        AttemptFrequency = other.AttemptFrequency;
        Broadcast = other.Broadcast;
        NeedHand = other.NeedHand;
        BreakOnHandChange = other.BreakOnHandChange;
        BreakOnDropItem = other.BreakOnDropItem;
        BreakOnMove = other.BreakOnMove;
        BreakOnWeightlessMove = other.BreakOnWeightlessMove;
        MovementThreshold = other.MovementThreshold;
        DistanceThreshold = other.DistanceThreshold;
        BreakOnDamage = other.BreakOnDamage;
        DamageThreshold = other.DamageThreshold;
        RequireCanInteract = other.RequireCanInteract;
        BlockDuplicate = other.BlockDuplicate;
        CancelDuplicate = other.CancelDuplicate;
        DuplicateCondition = other.DuplicateCondition;
    }
}

/// <summary>
/// See <see cref="DoAfterArgs.DuplicateCondition"/>.
/// </summary>
[Flags]
public enum DuplicateConditions : byte
{
    /// <summary>
    /// This DoAfter will consider any other DoAfter with the same user to be a duplicate.
    /// </summary>
    None = 0,

    /// <summary>
    /// Requires that <see cref="Used"/> refers to the same entity in order to be considered a duplicate.
    /// </summary>
    /// <remarks>
    /// E.g., if all checks are enabled for stripping, then stripping different articles of clothing on the same
    /// mob would be allowed. If instead this check were disabled, then any stripping actions on the same target
    /// would be considered duplicates, so you would only be able to take one piece of clothing at a time.
    /// </remarks>
    SameTool = 1 << 1,

    /// <summary>
    /// Requires that <see cref="Target"/> refers to the same entity in order to be considered a duplicate.
    /// </summary>
    /// <remarks>
    /// E.g., if all checks are enabled for mining, then using the same pickaxe to mine different rocks will be
    /// allowed. If instead this check were disabled, then the trying to mine a different rock with the same
    /// pickaxe would be considered a duplicate DoAfter.
    /// </remarks>
    SameTarget = 1 << 2,

    /// <summary>
    /// Requires that the <see cref="Event"/> types match in order to be considered a duplicate.
    /// </summary>
    /// <remarks>
    /// If your DoAfter should block other unrelated DoAfters involving the same set of entities, you may want
    /// to disable this condition. E.g. force feeding a donk pocket and forcefully giving someone a donk pocket
    /// should be mutually exclusive, even though the DoAfters have unrelated effects.
    /// </remarks>
    SameEvent = 1 << 3,

    All = SameTool | SameTarget | SameEvent,
}

/// <summary>
/// How often to raise <see cref="DoAfterAttemptEvent"/> on the user.
/// </summary>
[Serializable, NetSerializable]
public enum AttemptFrequency : byte
{
    /// <summary>
    /// Never raise the attempt event.
    /// </summary>
    Never = 0,

    /// <summary>
    /// Raises the attempt event when the DoAfter is about to start or end.
    /// </summary>
    StartAndEnd = 1,

    /// <summary>
    /// Raise the attempt event every tick while the DoAfter is running.
    /// </summary>
    EveryTick = 2
}
