using Robust.Shared.GameStates;

namespace Content.Shared.Tutorial;

/// <summary>
/// Marks a single tutorial step.
/// This entity will be spawned inside a container in the tutorial controller entity
/// and it will be deleted once the step is completed.
/// It listen to entities with <see cref="CompleteTutorialStepOnTriggerComponent"/>
/// broadcasting a <see cref="CompleteTutorialStepEvent"/> with the same ID and then tell the controller to continue with the next step.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialStepComponent : Component
{
    public const string StepStartKeyPrefix = "start_";
    public const string StepCompleteKeyPrefix = "complete_";

    [DataField(required: true), AutoNetworkedField]
    public string StepID = string.Empty;

    [DataField, AutoNetworkedField]
    public LocId? ChatMessage;

    /// <summary>
    /// Bool for ensuring we are not accidentally completing a step multiple times while it's queued for deletion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Complete;
}

