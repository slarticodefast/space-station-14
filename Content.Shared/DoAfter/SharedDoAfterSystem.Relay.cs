using Content.Shared.Damage.Systems;

namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<DoAfterComponent, DamageChangedEvent>(RelayEvent);

        // By-ref events
        //SubscribeLocalEvent<DoAfterComponent, ExampleEvent>(RefRelayEvent);
    }

    private void RelayEvent<T>(Entity<DoAfterComponent> ent, ref T args) where T : class
    {
        var ev = new DoAfterRelayedEvent<T>(args);

        foreach (var doAfterUid in ent.Comp.DoAfterContainer.ContainedEntities)
        {
            RaiseLocalEvent(doAfterUid, ref ev);
        }
    }

    private void RefRelayEvent<T>(Entity<DoAfterComponent> ent, ref T args) where T : struct
    {
        var ev = new DoAfterRelayedEvent<T>(args);

        foreach (var doAfterUid in ent.Comp.DoAfterContainer.ContainedEntities)
        {
            RaiseLocalEvent(doAfterUid, ref ev);
        }
        args = ev.Args; // copy back the result since structs are value types
    }
}

/// <summary>
/// Used to relay events from the player performing a DoAfter to the DoAfter entity.
/// </summary>
[ByRefEvent]
public record struct DoAfterRelayedEvent<TEvent>(TEvent Args);
