using Robust.Shared.GameStates;

namespace Content.Shared.DoAfter;

/// <summary>
/// Added to mobs that currently have DoAfter entities in the container given in their <see cref="DoAfterComponent"/>.
/// This does not necessarily mean the DoAfter is currently active - it could be completed or cancelled but
/// not despawned yet, see <see cref="SharedDoAfterSystem.ExcessTime"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveDoAfterComponent : Component;
