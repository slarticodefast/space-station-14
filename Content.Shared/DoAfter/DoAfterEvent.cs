using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

/// <summary>
/// Base type for events that get raised when a do-after is canceled or finished.
/// </summary>
[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class DoAfterEvent : HandledEntityEventArgs
{
    /// <summary>
    /// The DoAfterEntity that triggered this event. This will be set by the DoAfterSystem before the event is raised.
    /// So don't use this outside subscriptions to this event.
    /// Same for the shorthands for the user etc. below.
    /// </summary>
    [NonSerialized]
    public Entity<DoAfterEntityComponent> DoAfterEntity;

    //TODO: User pref to toggle repeat on specific doafters
    /// <summary>
    /// If set to true while handling this event, then the DoAfter will automatically be repeated.
    /// </summary>
    public bool Repeat = false;

    /// <summary>
    /// Duplicate the current event. This is used by state handling, and should copy by value unless the reference
    /// types are immutable.
    /// </summary>
    public abstract DoAfterEvent Clone();

    #region Convenience properties
    public bool Cancelled => DoAfterEntity.Comp.Cancelled;
    public EntityUid User => DoAfterEntity.Comp.User;
    public EntityUid? Target => DoAfterEntity.Comp.Target;
    public EntityUid? Used => DoAfterEntity.Comp.Used;
    public DoAfterArgs Args => DoAfterEntity.Comp.Args;
    #endregion

    /// <summary>
    /// Check whether this event is "the same" as another event for duplicate checking.
    /// </summary>
    public virtual bool IsDuplicate(DoAfterEvent other)
    {
        return GetType() == other.GetType();
    }
}

/// <summary>
/// Blank / empty event for simple do afters that carry no information.
/// </summary>
/// <remarks>
/// This just exists as a convenience to avoid having to re-implement Clone() for every simply DoAfterEvent.
/// If an event actually contains data, it should actually override Clone().
/// </remarks>
[Serializable, NetSerializable]
public abstract partial class SimpleDoAfterEvent : DoAfterEvent
{
    // TODO: Find some way to enforce that inheritors don't store data?
    // Alternatively, I just need to allow generics to be networked.
    // E.g., then a SimpleDoAfter<TEvent> would just raise a TEvent event.
    // But afaik generic event types currently can't be serialized for networking or YAML.

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// This event will optionally get raised every tick while a do-after is in progress to check whether the
/// DoAfter should be canceled.
/// </summary>
public sealed partial class DoAfterAttemptEvent<TEvent>(Entity<DoAfterEntityComponent> doAfter, TEvent @event) : CancellableEntityEventArgs where TEvent : DoAfterEvent
{
    /// <summary>
    /// The DoAfter that triggered this event.
    /// </summary>
    public readonly Entity<DoAfterEntityComponent> DoAfter = doAfter;

    /// <summary>
    /// The event that the DoAfter will raise after successfully finishing. Given that this event has the data
    /// required to perform the interaction, it should also contain the data required to validate/attempt the
    /// interaction.
    /// </summary>
    public readonly TEvent Event = @event;
}
