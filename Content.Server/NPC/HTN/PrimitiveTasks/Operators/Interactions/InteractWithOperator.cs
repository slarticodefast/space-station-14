using Content.Server.Interaction;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Timing;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

public sealed partial class InteractWithOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _doAfterSystem = sysManager.GetEntitySystem<SharedDoAfterSystem>();
    }

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = default!;

    /// <summary>
    /// Exit with failure if doafter wasn't raised
    /// </summary>
    [DataField]
    public bool ExpectDoAfter = false;

    public string CurrentDoAfter = "CurrentInteractWithDoAfter";


    // Ensure that CurrentDoAfter doesn't exist as we enter this operator,
    // the code currently relies on the result of a TryGetValue
    public override void Startup(NPCBlackboard blackboard)
    {
        blackboard.Remove<EntityUid>(CurrentDoAfter);
    }

    // Not really sure if we should clean it up, I guess some operator could use it
    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        blackboard.Remove<EntityUid>(CurrentDoAfter);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // Handle ongoing doAfter
        if (_entManager.TryGetComponent<DoAfterComponent>(owner, out var doAfter))
        {
            // if CurrentDoAfter contains something, we have an active doAfter
            if (blackboard.TryGetValue<EntityUid>(CurrentDoAfter, out var doAfterUid, _entManager))
            {
                if (!_entManager.EntityExists(doAfterUid))
                    return HTNOperatorStatus.Finished; // If the DoAfter entity is deleted it must have been finished.

                var status = _doAfterSystem.GetStatus(doAfterUid);
                return status switch
                {
                    DoAfterStatus.Running => HTNOperatorStatus.Continuing,
                    DoAfterStatus.Finished => HTNOperatorStatus.Finished,
                    _ => HTNOperatorStatus.Failed
                };
            }
        }

        if (_entManager.TryGetComponent<UseDelayComponent>(owner, out var useDelay) && _entManager.System<UseDelaySystem>().IsDelayed((owner, useDelay)) ||
            !blackboard.TryGetValue<EntityUid>(TargetKey, out var moveTarget, _entManager) ||
            !_entManager.TryGetComponent<TransformComponent>(moveTarget, out var targetXform))
        {
            return HTNOperatorStatus.Continuing;
        }

        if (_entManager.TryGetComponent<CombatModeComponent>(owner, out var combatMode))
        {
            _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, false, combatMode);
        }

        _entManager.System<InteractionSystem>().UserInteraction(owner, targetXform.Coordinates, moveTarget);

        // Detect doAfter, save it, and don't exit from this operator
        if (doAfter != null && doAfter.DoAfterContainer.Count != 0)
        {
            blackboard.SetValue(CurrentDoAfter, doAfter.DoAfterContainer.ContainedEntities[0]);
            return HTNOperatorStatus.Continuing;
        }

        // We shouldn't arrive here if we start a doafter, so fail if we expected a doafter
        if (ExpectDoAfter)
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
