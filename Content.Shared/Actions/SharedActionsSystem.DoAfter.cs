using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    protected void InitializeActionDoAfter()
    {
        SubscribeLocalEvent<DoAfterArgsComponent, ActionDoAfterEvent>(OnActionDoAfter);
    }

    private bool TryStartActionDoAfter(Entity<DoAfterArgsComponent> ent, Entity<DoAfterComponent?> performer, TimeSpan? originalUseDelay, RequestPerformActionEvent input)
    {
        if (!Resolve(performer, ref performer.Comp))
            return false;

        var actionDoAfterEvent = new ActionDoAfterEvent(originalUseDelay, input);

        var args = new DoAfterArgs(ent.Comp.DoAfterArgs)
        {
            Event = actionDoAfterEvent,
        };

        return _doAfter.TryStartDoAfter(ent.Comp.DoAfterArgs, performer, eventTarget: ent.Owner);
    }

    private void OnActionDoAfter(Entity<DoAfterArgsComponent> ent, ref ActionDoAfterEvent args)
    {
        if (!_actionQuery.TryComp(ent, out var actionComp))
            return;

        var action = (ent, actionComp);

        // If this DoAfter is on repeat and was cancelled, start use delay as expected
        if (args.Cancelled && ent.Comp.Repeat)
        {
            SetUseDelay(action, args.OriginalUseDelay);
            RemoveCooldown(action);
            StartUseDelay(action);
            UpdateAction(action);
            return;
        }

        args.Repeat = ent.Comp.Repeat;

        // Set the use delay to 0 so this can repeat properly
        if (ent.Comp.Repeat)
        {
            SetUseDelay(action, TimeSpan.Zero);
        }

        if (args.Cancelled)
            return;

        // Post original DoAfter, reduce the time on it now for other casts if ables
        if (ent.Comp.DelayReduction != null)
            args.Args.Delay = ent.Comp.DelayReduction.Value;

        // Validate again for charges, blockers, etc
        if (TryPerformAction(args.Input, args.User, skipDoActionRequest: true))
            return;

        // Cancel this DoAfter if we can't validate the action
        _doAfter.Cancel(args.DoAfterEntity.AsNullable(), force: true);
    }
}
