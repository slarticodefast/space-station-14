using Robust.Shared.GameStates;

namespace Content.Shared.Tutorial;

/// <summary>
/// Triggers this entity when a tutorial step was completed.
/// The trigger key will consist of the <see cref="TutorialStepComponent.StepStartKeyPrefix"/> and the step ID.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TriggerOnTutorialStepCompletedComponent : Component;
