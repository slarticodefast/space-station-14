using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from all containers
    /// </summary>
    [DataDefinition]
    public sealed partial class EmptyAllContainersBehaviour : IThresholdBehavior
    {
        public void Execute(EntityUid owner, IEntityManager entMan, DestructibleSystem system, EntityUid? cause = null)
        {
            if (!entMan.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            foreach (var container in entMan.System<SharedContainerSystem>().GetAllContainers(owner, containerManager))
            {
                system.ContainerSystem.EmptyContainer(container, true, entMan.GetComponent<TransformComponent>(owner).Coordinates);
            }
        }
    }
}
