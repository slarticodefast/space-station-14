using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<HandsComponent> _handsQuery;

    /// <summary>
    /// We'll use an excess time so stuff like finishing effects can show.
    /// </summary>
    public static readonly TimeSpan ExcessTime = TimeSpan.FromSeconds(0.5f);

    private static readonly ProtoId<TagPrototype> InstantDoAftersTag = "InstantDoAfters";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoAfterComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<DoAfterEntityComponent, DoAfterRelayedEvent<DamageChangedEvent>>(OnDamage);

        _xformQuery = GetEntityQuery<TransformComponent>();
        _handsQuery = GetEntityQuery<HandsComponent>();
    }

    private void OnRemovedFromContainer(Entity<DoAfterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // The change is already networked with the same game state.
        if (GameTiming.ApplyingState)
            return;

        if (args.Container.ID != DoAfterComponent.DoAfterContainerId)
            return;

        if (ent.Comp.DoAfterContainer.Count == 0)
            RemComp<ActiveDoAfterComponent>(ent.Owner); // No more DoAfter entities for this player.
    }

    /// <summary>
    /// Cancels DoAfter if it breaks on damage and it meets the threshold.
    /// </summary>
    private void OnDamage(Entity<DoAfterEntityComponent> ent, ref DoAfterRelayedEvent<DamageChangedEvent> args)
    {
        // The cancellation of the DoAfter is already networked in the same game state as the damage change.
        // So this prevents mispredicts.
        if (GameTiming.ApplyingState)
            return;

        if (!args.Args.InterruptsDoAfters || !args.Args.DamageIncreased || args.Args.DamageDelta == null)
            return;

        var delta = args.Args.DamageDelta.GetTotal();

        if (ent.Comp.Args.BreakOnDamage && delta >= ent.Comp.Args.DamageThreshold)
            Cancel(ent.AsNullable());
    }

    /// <summary>
    /// Raise the event given for the DoAfter.
    /// This happens either when the DoAfter finishes successfully or is cancelled.
    /// </summary>
    private void RaiseDoAfterEvents(Entity<DoAfterEntityComponent> ent)
    {
        var ev = ent.Comp.Args.Event;
        ev.Handled = false;
        ev.Repeat = false;
        // Fill this in so that subscriptions can use the shorthands for the user etc.
        ev.DoAfterEntity = ent;

        if (Exists(ent.Comp.EventTarget))
            RaiseLocalEvent(ent.Comp.EventTarget.Value, (object)ev, ent.Comp.Args.Broadcast);
        else if (ent.Comp.Args.Broadcast)
            RaiseLocalEvent((object)ev);
    }


    /// <summary>
    /// Cancel any applicable duplicate DoAfters and return whether or not the new DoAfter should be created.
    /// </summary>
    private bool ProcessDuplicates(DoAfterArgs args, Entity<DoAfterComponent> user, EntityUid? target, EntityUid? used)
    {
        var blocked = false;
        foreach (var existingUid in user.Comp.DoAfterContainer.ContainedEntities)
        {
            var existingComp = Comp<DoAfterEntityComponent>(existingUid);
            var existingUser = existingComp.User;
            var existingTarget = existingComp.Target;
            var existingUsed = existingComp.Used;

            if (existingComp.Cancelled || existingComp.Completed)
                continue;

            if (!IsDuplicate(
                existingComp.Args, args,
                existingTarget, target,
                existingUsed, used))
                continue;

            blocked = blocked | args.BlockDuplicate | existingComp.Args.BlockDuplicate;

            if (args.CancelDuplicate || existingComp.Args.CancelDuplicate)
                Cancel((existingUid, existingComp));
        }

        return !blocked;
    }

    private bool IsDuplicate(
        DoAfterArgs args, DoAfterArgs otherArgs,
        EntityUid? target, EntityUid? otherTarget,
        EntityUid? used, EntityUid? otherUsed)
    {
        if (IsDuplicate(
            args.DuplicateCondition,
            args, otherArgs,
            target, otherTarget,
            used, otherUsed))
            return true;

        // If both DoAfters have different conditions for being duplicates then check both.
        if (args.DuplicateCondition == otherArgs.DuplicateCondition)
            return false;

        return IsDuplicate(
            otherArgs.DuplicateCondition,
            args, otherArgs,
            target, otherTarget,
            used, otherUsed);
    }

    private bool IsDuplicate(
        DuplicateConditions conditions,
        DoAfterArgs args, DoAfterArgs otherArgs,
        EntityUid? target, EntityUid? otherTarget,
        EntityUid? used, EntityUid? otherUsed)
    {
        if ((conditions & DuplicateConditions.SameTarget) != 0
            && target != otherTarget)
        {
            return false;
        }

        if ((conditions & DuplicateConditions.SameTool) != 0
            && used != otherUsed)
        {
            return false;
        }

        if ((conditions & DuplicateConditions.SameEvent) != 0
            && !args.Event.IsDuplicate(otherArgs.Event))
        {
            return false;
        }

        return true;
    }
}
