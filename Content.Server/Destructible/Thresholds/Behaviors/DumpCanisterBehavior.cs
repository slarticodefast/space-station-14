using Content.Server.Atmos.Piping.Unary.EntitySystems;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class DumpCanisterBehavior : IThresholdBehavior
    {
        public void Execute(EntityUid owner, IEntityManager entMan, DestructibleSystem system, EntityUid? cause = null)
        {
            entMan.EntitySysManager.GetEntitySystem<GasCanisterSystem>().PurgeContents(owner);
        }
    }
}
