using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void PopupCursor(string? message, PopupType type = PopupType.Small)
        {
            // No local user.
        }

        public override void PopupCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupCursorEvent(message, type, tick), recipient);
        }

        public override void PopupCursor(string? message, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            if (!TryComp(recipient, out ActorComponent? actor))
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupCursorEvent(message, type, tick), actor.PlayerSession);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            var mapPos = _transform.ToMapCoordinates(coordinates);
            var filter = Filter.Empty().AddPlayersByPvs(mapPos, entManager: EntityManager, playerMan: _player, cfgMan: _cfg);
            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, tick, GetNetCoordinates(coordinates)), filter);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, Filter filter, bool replayRecord, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, tick, GetNetCoordinates(coordinates)), filter, replayRecord);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            if (!TryComp(recipient, out ActorComponent? actor))
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, tick, GetNetCoordinates(coordinates)), actor.PlayerSession);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, tick, GetNetCoordinates(coordinates)), recipient);
        }

        public override void PopupEntity(string? message, EntityUid uid, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            var filter = Filter.Empty().AddPlayersByPvs(uid, entityManager: EntityManager, playerMan: _player, cfgMan: _cfg);
            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupEntityEvent(message, type, tick, GetNetEntity(uid)), filter);

            Log.Debug($"server {Transform(uid).Coordinates}");
        }

        public override void PopupEntity(string? message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupEntityEvent(message, type, tick, GetNetEntity(uid)), filter, recordReplay);

            Log.Debug($"server {Transform(uid).Coordinates}");
        }

        public override void PopupEntity(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            if (!TryComp(recipient, out ActorComponent? actor))
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupEntityEvent(message, type, tick, GetNetEntity(uid)), actor.PlayerSession);

            Log.Debug($"server {Transform(uid).Coordinates}");
        }

        public override void PopupEntity(string? message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (message == null)
                return;

            var tick = _timing.CurTick;
            RaiseNetworkEvent(new PopupEntityEvent(message, type, tick, GetNetEntity(uid)), recipient);

            Log.Debug($"server {Transform(uid).Coordinates}");
        }

        public override void PopupEntity(string? recipientMessage, string? othersMessage, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            var tick = _timing.CurTick;
            var netId = GetNetEntity(uid);

            if (recipientMessage != null)
            {
                if (TryComp(recipient, out ActorComponent? actor))
                    RaiseNetworkEvent(new PopupEntityEvent(recipientMessage, type, tick, netId), actor.PlayerSession);
            }

            if (othersMessage != null)
            {
                // send to everyone in PVS range except the recipient
                var filter = Filter.Empty().AddPlayersByPvs(uid, entityManager: EntityManager, playerMan: _player, cfgMan: _cfg);
                if (recipient != null)
                    filter = filter.RemovePlayerByAttachedEntity(recipient.Value);
                RaiseNetworkEvent(new PopupEntityEvent(othersMessage, type, tick, netId), filter);
            }

            Log.Debug($"server {Transform(uid).Coordinates}");
        }
    }
}
