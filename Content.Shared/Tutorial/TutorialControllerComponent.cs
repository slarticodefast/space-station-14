using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tutorial;

/// <summary>
/// Entity that acts as a state machine for a single tutorial map and single player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialControllerComponent : Component
{
    /// <summary>
    /// The container id for the current tutorial step.
    /// </summary>
    public const string ContainerId = "tutorial_step_container";

    /// <summary>
    /// The container in which the current tutorial step entity is stored.
    /// </summary>
    [ViewVariables]
    public Container StepContainer = default!;

    /// <summary>
    /// The current step entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentStepEntity = null;

    /// <summary>
    /// Counter that keeps track of the current step.
    /// TODO: Replace with something like a construction graph so that we can have branching paths.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentStep = 1;

    /// <summary>
    /// List of step entity protoypes that will be used for the tutorial.
    /// If one is completed it will be deleted and the next one will be spawned inside the container.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Steps = new();
}
