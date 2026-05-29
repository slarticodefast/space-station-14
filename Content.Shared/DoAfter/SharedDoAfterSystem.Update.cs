namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = GameTiming.CurTime;

        var enumerator = EntityQueryEnumerator<DoAfterEntityComponent>();
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            Update((uid, comp), curTime);
        }
    }

    private void Update(Entity<DoAfterEntityComponent> ent, TimeSpan curTime)
    {
        if (ent.Comp.CancelledTime != null)
        {
            if (curTime - ent.Comp.CancelledTime.Value > ExcessTime)
                PredictedQueueDel(ent.Owner);
            return;
        }

        if (ent.Comp.Completed)
        {
            if (curTime - ent.Comp.StartTime > ent.Comp.Args.Delay + ExcessTime)
                PredictedQueueDel(ent.Owner);
            return;
        }

        if (ShouldCancel(ent))
        {
            Cancel(ent.AsNullable());
            return;
        }

        if (curTime - ent.Comp.StartTime >= ent.Comp.Args.Delay)
        {
            TryComplete(ent);
        }
    }

    private bool TryAttemptEvent(Entity<DoAfterEntityComponent> ent)
    {
        var args = ent.Comp.Args;
        // Fill this in so that subscriptions can use the shorthands for the user etc.
        args.Event.DoAfterEntity = ent;

        if (ent.Comp.AttemptEvent == null)
        {
            // I feel like this is somewhat cursed, but its the only way I can think of without having to just send
            // redundant data over the network and increasing DoAfter boilerplate.
            var evType = typeof(DoAfterAttemptEvent<>).MakeGenericType(args.Event.GetType());
            ent.Comp.AttemptEvent = _factory.CreateInstance(evType, new object[] { ent, args.Event });
        }

        args.Event.DoAfterEntity = ent;
        if (ent.Comp.EventTarget != null)
            RaiseLocalEvent(ent.Comp.EventTarget.Value, ent.Comp.AttemptEvent, args.Broadcast);
        else
            RaiseLocalEvent(ent.Comp.AttemptEvent);

        var ev = (CancellableEntityEventArgs)ent.Comp.AttemptEvent;
        if (!ev.Cancelled)
            return true;

        ev.Uncancel();
        return false;
    }

    private void TryComplete(Entity<DoAfterEntityComponent> ent)
    {
        if (ent.Comp.Cancelled || ent.Comp.Completed)
            return;

        // Perform final check (if required)
        if (ent.Comp.Args.AttemptFrequency == AttemptFrequency.StartAndEnd
            && !TryAttemptEvent(ent))
        {
            Cancel(ent.AsNullable());
            return;
        }

        ent.Comp.Completed = true;
        Dirty(ent);

        RaiseDoAfterEvents(ent);

        if (ent.Comp.Args.Event.Repeat)
        {
            ent.Comp.StartTime = GameTiming.CurTime;
            ent.Comp.Completed = false;
        }
    }

    private bool ShouldCancel(Entity<DoAfterEntityComponent> ent)
    {
        var args = ent.Comp.Args;

        if (!_xformQuery.TryGetComponent(ent.Comp.User, out var userXform))
            return true;

        //re-using xformQuery for Exists() checks.
        if (ent.Comp.Used is { Valid: true } used && !_xformQuery.HasComponent(used))
            return true;

        if (ent.Comp.EventTarget is { Valid: true } eventTarget && !_xformQuery.HasComponent(eventTarget))
            return true;

        TransformComponent? targetXform = null;
        if (ent.Comp.Target is { Valid: true } target && !_xformQuery.TryGetComponent(target, out targetXform))
            return true;

        if (args.BreakOnMove && !(!args.BreakOnWeightlessMove && _gravity.IsWeightless(ent.Comp.User)))
        {
            // Whether the user has moved too much from their original position.
            if (!_transform.InRange(userXform.Coordinates, ent.Comp.UserPosition, args.MovementThreshold))
                return true;

            // Whether the distance between the user and target(if any) has changed too much.
            if (targetXform != null &&
                targetXform.Coordinates.TryDistance(EntityManager, userXform.Coordinates, out var distance))
            {
                if (Math.Abs(distance - ent.Comp.TargetDistance) > args.MovementThreshold)
                    return true;
            }
        }

        // Whether the user and the target are too far apart.
        if (ent.Comp.Target != null)
        {
            if (args.DistanceThreshold != null)
            {
                if (!_interaction.InRangeAndAccessible(ent.Comp.User, ent.Comp.Target.Value, args.DistanceThreshold.Value))
                    return true;
            }
        }

        // Whether the distance between the tool and the user has grown too much.
        if (ent.Comp.Used != null)
        {
            if (args.DistanceThreshold != null)
            {
                if (!_interaction.InRangeUnobstructed(ent.Comp.User,
                        ent.Comp.Used.Value,
                        args.DistanceThreshold.Value))
                    return true;
            }
        }

        if (args.AttemptFrequency == AttemptFrequency.EveryTick && !TryAttemptEvent(ent))
            return true;

        // Check if the DoAfter requires hands to perform at first
        // For example, you need hands to strip clothes off of someone
        // This does not mean their hand needs to be empty.
        if (args.NeedHand)
        {
            if (!_handsQuery.TryGetComponent(ent.Comp.User, out var hands) || hands.Count == 0)
                return true;

            // If an item was in the user's hand to begin with,
            // check if the user is no longer holding the item.
            if (args.BreakOnDropItem && ent.Comp.InitialItem != null && !_hands.IsHolding((ent.Comp.User, hands), ent.Comp.InitialItem))
                return true;

            // If the user changes which hand is active at all, interrupt the do-after
            if (args.BreakOnHandChange && hands.ActiveHandId != ent.Comp.InitialHand)
                return true;
        }

        if (args.RequireCanInteract && !_actionBlocker.CanInteract(ent.Comp.User, ent.Comp.Target))
            return true;

        return false;
    }
}
