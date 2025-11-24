using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Tutorial;

public sealed class TutorialSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TutorialControllerComponent, ComponentStartup>(OnControllerStartup);
        SubscribeLocalEvent<TutorialControllerComponent, MapInitEvent>(OnControllerInit);

        SubscribeLocalEvent<TutorialStepComponent, MapInitEvent>(OnStepInit);

        SubscribeLocalEvent<CompleteTutorialStepEvent>(OnCompleteTutorialStep);
    }

    private void OnControllerStartup(Entity<TutorialControllerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.StepContainer = _container.EnsureContainer<Container>(ent.Owner, TutorialControllerComponent.ContainerId);
    }

    private void OnControllerInit(Entity<TutorialControllerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.CurrentStep > ent.Comp.Steps.Count)
            Log.Error("Tutorial step counter was larger than the step count.");
        else // spawn the first tutorial step
            TrySpawnInContainer(
                ent.Comp.Steps[ent.Comp.CurrentStep - 1],
                ent.Owner,
                TutorialControllerComponent.ContainerId,
                out ent.Comp.CurrentStepEntity);

        // TODO: Add a PVS override to players on the same map, so that the tutorial remains predicted, even if out of PVS range.
    }

    private void OnStepInit(Entity<TutorialStepComponent> ent, ref MapInitEvent args)
    {
        StartStep(ent);
    }

    private void OnCompleteTutorialStep(ref CompleteTutorialStepEvent args)
    {
        // Get the map the event was raised on.
        var mapID = Transform(args.Uid).MapID;

        // Iterate all controller entities to check which one to complete
        var controllerQuery = EntityQueryEnumerator<TutorialStepComponent, TransformComponent>();
        while (controllerQuery.MoveNext(out var uid, out var stepComp, out var transformComp))
        {
            // Only consider the controller on the same map since we want the step completed for the correct player.
            if (transformComp.MapID != mapID)
                continue;

            // Did this complete the step we are currently on?
            if (args.StepID != stepComp.StepID)
                continue;

            CompleteStep((uid, stepComp), args.User);
        }
    }

    /// <summary>
    /// Start this tutorial step, triggering other entities as needed.
    /// </summary>
    public void StartStep(Entity<TutorialStepComponent> ent)
    {
        var mapID = Transform(ent).MapID;
        _adminLog.Add(LogType.Tutorial, $"Started tutorial step {ent.Owner} on map {mapID}"); // TODO: Use map entity so it shows the player name.

        // Trigger all tutorial entities on the map the step is on.
        var triggerQuery = EntityQueryEnumerator<TriggerOnTutorialStepStartedComponent, TransformComponent>();
        while (triggerQuery.MoveNext(out var uid, out _, out var transformComp))
        {
            if (transformComp.MapID != mapID)
                continue;

            _trigger.Trigger(uid, null, TutorialStepComponent.StepStartKeyPrefix + ent.Comp.StepID);
        }

        if (ent.Comp.ChatMessage == null)
            return;

        // Show a chat message telling the player what to do.
        // TODO: Make this map-wide instead of local chat.
        // How do I even do that? Add a new radio channel? I hate chat code.
        _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString(ent.Comp.ChatMessage.Value), InGameICChatType.Speak, false, hideLog: true);
    }

    /// <summary>
    /// Complete this tutorial step, triggering other entities as needed.
    /// </summary>
    public void CompleteStep(Entity<TutorialStepComponent> ent, EntityUid? user)
    {
        if (ent.Comp.Complete) // Make sure we are not accidentally completing the step multiple times while it's already queued for deletion.
            return;

        ent.Comp.Complete = true;
        Dirty(ent);

        var mapID = Transform(ent).MapID;
        _adminLog.Add(LogType.Tutorial, $"Tutorial step {ent.Owner} on map {mapID} was completed by {user}"); // TODO: Use map entity so it shows the player name.

        // Trigger all tutorial entities on the map the step is on.
        var triggerQuery = EntityQueryEnumerator<TriggerOnTutorialStepCompletedComponent, TransformComponent>();
        while (triggerQuery.MoveNext(out var uid, out _, out var transformComp))
        {
            if (transformComp.MapID != mapID)
                continue;

            _trigger.Trigger(uid, null, TutorialStepComponent.StepStartKeyPrefix + ent.Comp.StepID);
        }


        // Get the tutorial controller for this step.
        var controllerUid = Transform(ent).ParentUid;

        // Remove the old step.
        PredictedQueueDel(ent.Owner);

        if (!TryComp<TutorialControllerComponent>(controllerUid, out var controllerComp))
        {
            Log.Error($"Tutorial step {ent.Owner} was not inside a tutorial controller container.");
            return;
        }

        controllerComp.CurrentStep++;
        Dirty(controllerUid, controllerComp);

        if (controllerComp.CurrentStep > controllerComp.Steps.Count)
            return; // Last step reached.

        // Spawn the next step.
        if (!PredictedTrySpawnInContainer(
            controllerComp.Steps[controllerComp.CurrentStep - 1],
            ent.Owner,
            TutorialControllerComponent.ContainerId,
            out controllerComp.CurrentStepEntity))
            return;
    }
}

public sealed class CompleteTutorialStepOnTriggerComponentOnTriggerSystem : XOnTriggerSystem<CompleteTutorialStepOnTriggerComponent>
{
    protected override void OnTrigger(Entity<CompleteTutorialStepOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var ev = new CompleteTutorialStepEvent(ent.Owner, args.User, ent.Comp.StepID);
        RaiseLocalEvent(ref ev);
        args.Handled = true;
    }
}
