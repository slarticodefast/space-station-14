using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Popups
{
    /// <summary>
    /// System for displaying small text popups on users' screens.
    /// </summary>
    public abstract class SharedPopupSystem : EntitySystem
    {
        /// <summary>
        /// Shows a popup at the local users' cursor. Does nothing on the server.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCursor(string? message, PopupType type = PopupType.Small);

        /// <summary>
        /// Shows a popup at a users' cursor.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="recipient">Client that will see this popup.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small);

        /// <summary>
        /// Shows a popup at a users' cursor.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="recipient">Client that will see this popup.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCursor(string? message, EntityUid? recipient, PopupType type = PopupType.Small);

        /// <summary>
        /// Shows a popup at a world location to every entity in PVS range.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="coordinates">The coordinates where to display the message.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, PopupType type = PopupType.Small);

        /// <summary>
        /// Filtered variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/>, which should only be used
        /// if the filtering has to be more specific than simply PVS range based.
        /// </summary>
        /// <param name="filter">Filter for the players that will see the popup.</param>
        /// <param name="recordReplay">If true, this pop-up will be considered as a globally visible pop-up that gets shown during replays.</param>
        public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, Filter filter, bool recordReplay, PopupType type = PopupType.Small);

        /// <summary>
        /// Variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/> that sends a pop-up to the player attached to some entity.
        /// </summary>
        public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small);

        /// <summary>
        /// Variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/> that sends a pop-up to a specific player session.
        /// </summary>
        public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small);

        /// <summary>
        /// Shows a popup above an entity for every player in PVS range.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="uid">The UID of the entity.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupEntity(string? message, EntityUid uid, PopupType type = PopupType.Small);

        /// <summary>
        /// Filtered variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/>, which should only be used
        /// if the filtering has to be more specific than simply PVS range based.
        /// </summary>
        public abstract void PopupEntity(string? message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small);

        /// <summary>
        /// Variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/> that shows the popup only to the player attached to some entity.
        /// </summary>
        public abstract void PopupEntity(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small);

        /// <summary>
        /// Variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/> that shows the popup only to a specific player session.
        /// </summary>
        public abstract void PopupEntity(string? message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small);

        /// <summary>
        /// Variant of <see cref="PopupEntity(string?, EntityUid, EntityUid?, PopupType)"/> that displays <paramref name="recipientMessage"/>
        /// to the recipient and <paramref name="othersMessage"/> to everyone else in PVS range.
        /// </summary>
        public abstract void PopupEntity(string? recipientMessage, string? othersMessage, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small);

        [Obsolete("Use PopupCursor. Popups are automatically predicted now.")]
        public void PopupPredictedCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            PopupCursor(message, recipient, type);
        }

        [Obsolete("Use PopupCursor. Popups are automatically predicted now.")]
        public void PopupPredictedCursor(string? message, EntityUid recipient, PopupType type = PopupType.Small)
        {
            PopupCursor(message, recipient, type);
        }

        [Obsolete("Use PopupCoordinates. Popups are automatically predicted now.")]
        public void PopupPredictedCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            PopupCoordinates(message, coordinates, recipient);
        }

        [Obsolete("Use PopupCursor. Popups are automatically predicted now.")]
        public void PopupClient(string? message, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            PopupCursor(message, recipient, type);
        }

        [Obsolete("Use PopupEntity. Popups are automatically predicted now.")]
        public void PopupClient(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            PopupEntity(message, uid, recipient);
        }

        [Obsolete("Use PopupCoordinates. Popups are automatically predicted now.")]
        public void PopupClient(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            PopupCoordinates(message, coordinates, recipient, type);
        }

        [Obsolete("Use PopupEntity. Popups are automatically predicted now.")]
        public void PopupPredicted(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            PopupEntity(message, uid, recipient, type);
        }

        [Obsolete("Use PopupEntity. Popups are automatically predicted now.")]
        public void PopupPredicted(string? message, EntityUid uid, EntityUid? recipient, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
        {
            PopupEntity(message, uid, filter, recordReplay, type);
        }

        [Obsolete("Use PopupEntity. Popups are automatically predicted now.")]
        public void PopupPredicted(string? recipientMessage, string? othersMessage, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            PopupEntity(recipientMessage, othersMessage, uid, recipient, type);
        }
    }

    /// <summary>
    /// Common base for all popup network events.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class PopupEvent(
        string message,
        PopupType type,
        GameTick tick) : EntityEventArgs
    {
        public string Message { get; } = message;

        public PopupType Type { get; } = type;

        public GameTick Tick { get; } = tick;
    }

    /// <summary>
    /// Network event for displaying a popup on the user's cursor.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PopupCursorEvent(
        string message,
        PopupType type,
        GameTick tick) : PopupEvent(message, type, tick);

    /// <summary>
    /// Network event for displaying a popup at a world location.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PopupCoordinatesEvent(
        string message,
        PopupType type,
        GameTick tick,
        NetCoordinates coordinates) : PopupEvent(message, type, tick)
    {
        public NetCoordinates Coordinates { get; } = coordinates;
    }

    /// <summary>
    /// Network event for displaying a popup above an entity.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PopupEntityEvent(
        string message,
        PopupType type,
        GameTick tick,
        NetEntity uid) : PopupEvent(message, type, tick)
    {
        public NetEntity Uid { get; } = uid;
    }

    /// <summary>
    /// Used to determine how a popup should appear visually to the client. Caution variants simply have a red color.
    /// </summary>
    /// <remarks>
    /// Actions which can fail or succeed should use a smaller popup for failure and a larger popup for success.
    /// Actions which have different popups for the user vs. others should use a larger popup for the user and a smaller popup for others.
    /// Actions which result in harm or are otherwise dangerous should always show as the caution variant.
    /// </remarks>
    [Serializable, NetSerializable]
    public enum PopupType : byte
    {
        /// <summary>
        /// Small popups are the default, and denote actions that may be spammable or are otherwise unimportant.
        /// </summary>
        Small,
        SmallCaution,
        /// <summary>
        /// Medium popups should be used for actions which are not spammable but may not be particularly important.
        /// </summary>
        Medium,
        MediumCaution,
        /// <summary>
        /// Large popups should be used for actions which may be important or very important to one or more users,
        /// but is not life-threatening.
        /// </summary>
        Large,
        LargeCaution,
    }

    /// <summary>
    /// Enum for storing information of which API method was used to create the popup.
    /// Needed creating unique hashes.
    /// </remarks>
    [Serializable, NetSerializable]
    public enum PopupMethod : byte
    {
        PopupCursor,
        PopupCoordinates,
        PopupEntity,
    }
}
