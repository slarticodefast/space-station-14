using Content.Shared.Hands.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Hands.EntitySystems;

public sealed class ExtraHandsEquipmentSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraHandsEquipmentComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ExtraHandsEquipmentComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<ExtraHandsEquipmentComponent> ent, ref GotEquippedEvent args)
    {
        if (!TryComp<HandsComponent>(args.Equipee, out var handsComp))
            return;

        for (int i = 0; i < ent.Comp.HandAmount; i++)
        {
            var handName = $"{GetNetEntity(ent.Owner).Id}-extra-{i}";
            Log.Debug($"add hand {handName} {args.Equipee}");
            _hands.AddHand(args.Equipee, handName, HandLocation.Middle, handsComp);
            ent.Comp.HandNames.Add(handName);
        }
    }

    private void OnUnequipped(Entity<ExtraHandsEquipmentComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<HandsComponent>(args.Equipee, out var handsComp))
            return;

        foreach (var handName in ent.Comp.HandNames)
            _hands.RemoveHand(args.Equipee, handName, handsComp);
    }
}
