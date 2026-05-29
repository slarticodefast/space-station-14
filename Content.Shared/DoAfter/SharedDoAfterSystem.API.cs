using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem
{
    #region Creation

    /// <summary>
    /// Attempts to start a new DoAfter. Note that even if this function returns true, an interaction may have
    /// occured, as starting a duplicate DoAfter may cancel currently running DoAfters.
    /// </summary>
    /// <param name="args">The DoAfter arguments.</param>
    /// <param name="user">The user performing the DoAfter.</param>
    /// <param name="target">The entity targeted by the DoAfter.</param>
    /// <param name="used">The entity used to perform the DoAfter (for example a tool).</param>
    /// <param name="eventTarget">The entity the event given in the DoAfterArgs will be raised on. Defaults to the user.</param>
    /// <returns>True if the DoAfter was started.</returns>
    /// <remarks>
    /// The eventTarget does not default to null so that you have to deliberately set it to null to prevent user errors where
    /// you forget to do so and wonder why your subscription is not working.
    /// </remarks>
    public bool TryStartDoAfter(DoAfterArgs args, Entity<DoAfterComponent?> user, EntityUid? eventTarget, EntityUid? target = null, EntityUid? used = null)
    {
        return TryStartDoAfter(args, out _, user, eventTarget, target, used);
    }

    /// <summary>
    /// Attempts to start a new DoAfter. Note that even if this function returns true, an interaction may have
    /// occured, as starting a duplicate DoAfter may cancel currently running DoAfters.
    /// </summary>
    /// <param name="args">The DoAfter arguments.</param>
    /// <param name="doAfterEnt">The newly started DoAfterEntity.</param>
    /// <param name="user">The user performing the DoAfter.</param>
    /// <param name="target">The entity targeted by the DoAfter.</param>
    /// <param name="used">The entity used to perform the DoAfter (for example a tool).</param>
    /// <param name="eventTarget">The entity the event given in the DoAfterArgs will be raised on. Defaults to the user.</param>
    /// <returns>True if the DoAfter was started.</returns>
    public bool TryStartDoAfter(DoAfterArgs args, [NotNullWhen(true)] out Entity<DoAfterEntityComponent>? doAfterEnt, Entity<DoAfterComponent?> user, EntityUid? eventTarget, EntityUid? target = null, EntityUid? used = null)
    {
        DebugTools.Assert(args.Event.GetType().HasCustomAttribute<NetSerializableAttribute>()
            || args.Event.GetType().Namespace is { } ns && ns.StartsWith("Content.IntegrationTests"), // classes defined in tests cannot be marked as serializable.
            $"DoAfter event is not serializable. Event: {args.Event.GetType()}");

        doAfterEnt = null;

        if (!Resolve(user, ref user.Comp))
            return false;

        // Duplicate blocking & cancellation.
        if (!ProcessDuplicates(args, user!, target, used))
            return false;

        if (!PredictedTrySpawnInContainer(args.Prototype, user.Owner, DoAfterComponent.DoAfterContainerId, out var spawned))
        {
            Log.Error($"Unable to spawn DoAfter entity for user {ToPrettyString(user.Owner)}");
            return false;
        }

        if (!TryComp<DoAfterEntityComponent>(spawned, out var spawnedComp))
        {
            Log.Error($"DoAfter entity {ToPrettyString(spawned)} did not have a DoAfterEntityComponent");
            PredictedDel(spawned);
            return false;
        }

        doAfterEnt = (spawned.Value, spawnedComp);

        Dirty(doAfterEnt.Value);
        spawnedComp.Args = args;
        spawnedComp.User = user.Owner;
        spawnedComp.Target = target;
        spawnedComp.Used = used;
        spawnedComp.EventTarget = eventTarget;
        spawnedComp.StartTime = GameTiming.CurTime;
        if (args.BreakOnMove)
            spawnedComp.UserPosition = Transform(user).Coordinates;

        if (target != null && args.BreakOnMove)
        {
            var targetPosition = Transform(target.Value).Coordinates;
            spawnedComp.UserPosition.TryDistance(EntityManager, targetPosition, out spawnedComp.TargetDistance);
        }

        // For this we need to stay on the same hand slot and need the same item in that hand slot
        // (or if there is no item there we need to keep it free).
        if (args.NeedHand && (args.BreakOnHandChange || args.BreakOnDropItem))
        {
            if (!TryComp<HandsComponent>(user, out var handsComponent))
            {
                PredictedDel(spawned);
                return false;
            }

            spawnedComp.InitialHand = handsComponent.ActiveHandId;
            spawnedComp.InitialItem = _hands.GetActiveItem((user.Owner, handsComponent));
        }

        // Initial checks
        if (ShouldCancel(doAfterEnt.Value))
        {
            PredictedDel(spawned);
            return false;
        }

        if (args.AttemptFrequency == AttemptFrequency.StartAndEnd && !TryAttemptEvent(doAfterEnt.Value))
        {
            PredictedDel(spawned);
            return false;
        }

        EnsureComp<ActiveDoAfterComponent>(user);

        // TODO DO AFTER
        // Why does this tag exist? Just make this a bool on the component?
        if (args.Delay <= TimeSpan.Zero || _tag.HasTag(user, InstantDoAftersTag))
        {
            TryComplete(doAfterEnt.Value);
        }

        return true;
    }

    #endregion

    #region Cancellation
    /// <summary>
    /// Cancels an active DoAfter entity.
    /// </summary>
    public void Cancel(EntityUid? uid, bool force = false)
    {
        if (uid == null)
            return;

        if (TryComp<DoAfterEntityComponent>(uid, out var comp))
            Cancel((uid.Value, comp), force);
        else
            Log.Error($"DoAfterEntity {ToPrettyString(uid.Value)} did not have a DoAfterEntityComponent.");
    }

    /// <summary>
    /// Cancels an active DoAfter entity.
    /// Will cause a warning if the DoAfter entity already finished and deleted itself,
    /// so make sure the entity still exists.
    /// </summary>
    public void Cancel(Entity<DoAfterEntityComponent?> ent, bool force = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Cancelled || (ent.Comp.Completed && !force))
            return;

        ent.Comp.CancelledTime = GameTiming.CurTime;
        Dirty(ent);
        RaiseDoAfterEvents((ent.Owner, ent.Comp));
    }

    #endregion

    #region Query
    /// <summary>
    /// Returns the current status of a DoAfter entity.
    /// </summary>
    public DoAfterStatus GetStatus(Entity<DoAfterEntityComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return DoAfterStatus.Invalid;

        if (ent.Comp.Cancelled)
            return DoAfterStatus.Cancelled;

        if (!ent.Comp.Completed)
            return DoAfterStatus.Running;

        // Theres the chance here that the DoAfter hasn't actually finished yet if the system's update hasn't run yet.
        // This would also mean the post-DoAfter checks haven't run yet. But whatever, I can't be bothered tracking and
        // networking whether a do-after has raised its events or not.
        return DoAfterStatus.Finished;
    }

    /// <summary>
    /// Returns if the DoAfter entity is currently running.
    /// </summary>
    public bool IsRunning(Entity<DoAfterEntityComponent?> ent)
    {
        return GetStatus(ent) == DoAfterStatus.Running;
    }

    /// <summary>
    /// Returns if the DoAfter entity was cancelled.
    /// </summary>
    public bool IsCancelled(Entity<DoAfterEntityComponent?> ent)
    {
        return GetStatus(ent) == DoAfterStatus.Cancelled;
    }
    #endregion
}
