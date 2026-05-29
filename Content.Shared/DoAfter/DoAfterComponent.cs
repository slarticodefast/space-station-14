using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoAfterSystem))]
public sealed partial class DoAfterComponent : Component
{
    /// <summary>
    /// The id of the container to store doafter entities in.
    /// </summary>
    public const string DoAfterContainerId = "doafters";

    /// <summary>
    /// The container to store doafter entities in.
    /// </summary>
    [ViewVariables]
    public Container DoAfterContainer = default!;
}


/// <summary>
/// The state of a DoAfter.
/// </summary>
[Serializable, NetSerializable]
public enum DoAfterStatus : byte
{
    Invalid,
    Running,
    Cancelled,
    Finished,
}
