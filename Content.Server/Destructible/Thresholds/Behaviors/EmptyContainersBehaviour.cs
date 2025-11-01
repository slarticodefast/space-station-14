using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from specified containers
    /// </summary>
    [DataDefinition]
    public sealed partial class EmptyContainersBehaviour : IThresholdBehavior
    {
        [DataField("containers")]
        public List<string> Containers = new();

        public void Execute(EntityUid owner, IEntityManager entMan, DestructibleSystem system, EntityUid? cause = null)
        {
            if (!entMan.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            var containerSys = entMan.System<ContainerSystem>();


            foreach (var containerId in Containers)
            {
                if (!containerSys.TryGetContainer(owner, containerId, out var container, containerManager))
                    continue;

                containerSys.EmptyContainer(container, true);
            }
        }
    }
}
