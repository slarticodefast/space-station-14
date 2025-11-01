using System.Numerics;
using Content.Server.Forensics;
using Content.Server.Stack;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class SpawnEntitiesBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Entities spawned on reaching this threshold, from a min to a max.
        /// </summary>
        [DataField]
        public Dictionary<EntProtoId, MinMax> Spawn = new();

        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        [DataField("transferForensics")]
        public bool DoTransferForensics;

        [DataField]
        public bool SpawnInContainer;

        public void Execute(EntityUid owner, IEntityManager entMan, DestructibleSystem system, EntityUid? cause = null)
        {
            var tSys = entMan.System<TransformSystem>();
            var position = tSys.GetMapCoordinates(owner);

            var getRandomVector = () => new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));

            var executions = 1;
            if (entMan.TryGetComponent<StackComponent>(owner, out var stack))
            {
                executions = stack.Count;
            }

            foreach (var (entityId, minMax) in Spawn)
            {
                for (var execution = 0; execution < executions; execution++)
                {
                    var count = minMax.Min >= minMax.Max
                        ? minMax.Min
                        : system.Random.Next(minMax.Min, minMax.Max + 1);

                    if (count == 0)
                        continue;

                    if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, system.PrototypeManager, entMan.ComponentFactory))
                    {
                        var spawned = SpawnInContainer
                            ? entMan.SpawnNextToOrDrop(entityId, owner)
                            : entMan.SpawnEntity(entityId, position.Offset(getRandomVector()));
                        system.StackSystem.SetCount((spawned, null), count);

                        TransferForensics(spawned, entMan, system, owner);
                    }
                    else
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var spawned = SpawnInContainer
                                ? entMan.SpawnNextToOrDrop(entityId, owner)
                                : entMan.SpawnEntity(entityId, position.Offset(getRandomVector()));

                            TransferForensics(spawned, entMan, system, owner);
                        }
                    }
                }
            }
        }

        public void TransferForensics(EntityUid spawned, IEntityManager entMan, DestructibleSystem system, EntityUid owner)
        {
            if (!DoTransferForensics ||
                !entMan.TryGetComponent<ForensicsComponent>(owner, out var forensicsComponent))
                return;

            var comp = entMan.EnsureComponent<ForensicsComponent>(spawned);
            comp.DNAs = forensicsComponent.DNAs;

            if (!system.Random.Prob(0.4f))
                return;
            comp.Fingerprints = forensicsComponent.Fingerprints;
            comp.Fibers = forensicsComponent.Fibers;
        }
    }
}
