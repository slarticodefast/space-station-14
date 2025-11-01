using Content.Shared.Medical;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class VomitBehavior : IThresholdBehavior
{
    public void Execute(EntityUid uid, IEntityManager entMan, DestructibleSystem system, EntityUid? cause = null)
    {
        entMan.System<VomitSystem>().Vomit(uid);
    }
}
