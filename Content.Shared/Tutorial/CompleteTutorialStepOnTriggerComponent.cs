using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared.Tutorial;

/// <summary>
/// Broadcasts a <see cref="CompleteTutorialStepEvent"/> when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CompleteTutorialStepOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The id of the tutorial step that will be completed.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string StepID = string.Empty;
}

/// <summary>
/// Raised when a player does an interaction that completes a tutorial step.
/// </summary>
[ByRefEvent]
public record struct CompleteTutorialStepEvent(EntityUid Uid, EntityUid? User, string StepID);
