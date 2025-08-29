using System.Linq;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.Timing;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Client.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IOverlayManager _overlay = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public IReadOnlyCollection<WorldPopupLabel> WorldLabels => _aliveWorldLabels.Values;
        public IReadOnlyCollection<CursorPopupLabel> CursorLabels => _aliveCursorLabels.Values;

        private readonly Dictionary<WorldPopupData, WorldPopupLabel> _aliveWorldLabels = new();
        private readonly Dictionary<CursorPopupData, CursorPopupLabel> _aliveCursorLabels = new();

        public const float MinimumPopupLifetime = 0.7f;
        public const float MaximumPopupLifetime = 5f;
        public const float PopupLifetimePerCharacter = 0.04f;

        // list of popup hashes that we predicted since the last incoming server message
        private HashSet<PopupHash> _predictedPopups = new();

        public override void Initialize()
        {
            SubscribeNetworkEvent<PopupCursorEvent>(OnPopupCursorEvent);
            SubscribeNetworkEvent<PopupCoordinatesEvent>(OnPopupCoordinatesEvent);
            SubscribeNetworkEvent<PopupEntityEvent>(OnPopupEntityEvent);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
            _overlay.AddOverlay(new PopupOverlay(
                _configManager,
                EntityManager,
                _playerManager,
                _prototype,
                _uiManager,
                _uiManager.GetUIController<PopupUIController>(),
                _examine,
                _transform,
                this));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlay.RemoveOverlay<PopupOverlay>();
        }

        private void WrapAndRepeatPopup(PopupLabel existingLabel, string popupMessage)
        {
            existingLabel.TotalTime = 0;
            existingLabel.Repeats += 1;
            existingLabel.Text = Loc.GetString("popup-system-repeated-popup-stacking-wrap",
                ("popup-message", popupMessage),
                ("count", existingLabel.Repeats));
        }

        private void PopupInternal(
            string? message,
            PopupType type,
            EntityCoordinates coordinates,
            EntityUid? entity,
            bool recordReplay,
            GameTick tick)
        {
            if (message == null)
                return;

            // If the popup was already shown through prediction don't show it again
            var hash = new PopupHash(message, entity, coordinates, type, tick);
            if (!_predictedPopups.Add(hash))
                return;

            Log.Debug($"client {coordinates}");
            //Log.Debug($"{coordinates.GetHashCode()} {coordinates.X} {coordinates.Y} {coordinates.EntityId}");

            if (recordReplay && _replayRecording.IsRecording)
            {
                if (entity != null)
                    _replayRecording.RecordClientMessage(new PopupEntityEvent(message, type, _timing.CurTick, GetNetEntity(entity.Value)));
                else
                    _replayRecording.RecordClientMessage(new PopupCoordinatesEvent(message, type, _timing.CurTick, GetNetCoordinates(coordinates)));
            }

            var popupData = new WorldPopupData(message, type, coordinates, entity);
            if (_aliveWorldLabels.TryGetValue(popupData, out var existingLabel))
            {
                WrapAndRepeatPopup(existingLabel, popupData.Message);
                return;
            }

            var label = new WorldPopupLabel(coordinates)
            {
                Text = message,
                Type = type,
            };

            _aliveWorldLabels.Add(popupData, label);
        }

        private void PopupCursorInternal(
            string? message,
            PopupType type,
            bool recordReplay,
            GameTick tick)
        {
            if (message == null)
                return;

            // If the popup was already shown through prediction don't show it again
            if (!_predictedPopups.Add(new PopupHash(message, null, null, type, tick)))
                return;

            if (recordReplay && _replayRecording.IsRecording)
                _replayRecording.RecordClientMessage(new PopupCursorEvent(message, type, _timing.CurTick));

            var popupData = new CursorPopupData(message, type);
            if (_aliveCursorLabels.TryGetValue(popupData, out var existingLabel))
            {
                WrapAndRepeatPopup(existingLabel, popupData.Message);
                return;
            }

            var label = new CursorPopupLabel(_inputManager.MouseScreenPosition)
            {
                Text = message,
                Type = type,
            };

            _aliveCursorLabels.Add(popupData, label);
        }

        #region Abstract Method Implementations
        public override void PopupCursor(string? message, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            PopupCursorInternal(message, type, true, _timing.CurTick);
        }

        public override void PopupCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (_playerManager.LocalSession == recipient)
                PopupCursorInternal(message, type, true, _timing.CurTick);
        }

        public override void PopupCursor(string? message, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (recipient != null && _playerManager.LocalEntity == recipient)
                PopupCursorInternal(message, type, true, _timing.CurTick);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            PopupInternal(message, type, coordinates, null, true, _timing.CurTick);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, Filter filter, bool replayRecord, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (filter.Recipients.Contains(_playerManager.LocalSession))
                PopupInternal(message, type, coordinates, null, replayRecord, _timing.CurTick);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (recipient != null && _playerManager.LocalEntity == recipient)
                PopupInternal(message, type, coordinates, null, true, _timing.CurTick);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (_playerManager.LocalSession == recipient)
                PopupInternal(message, type, coordinates, null, true, _timing.CurTick);
        }

        public override void PopupEntity(string? message, EntityUid uid, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (TryComp(uid, out TransformComponent? transform))
                PopupInternal(message, type, transform.Coordinates, uid, true, _timing.CurTick);
        }

        public override void PopupEntity(string? message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (filter.Recipients.Contains(_playerManager.LocalSession)
                && TryComp(uid, out TransformComponent? transform))
                PopupInternal(message, type, transform.Coordinates, uid, recordReplay, _timing.CurTick);
        }

        public override void PopupEntity(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (recipient != null
                && _playerManager.LocalEntity == recipient
                && TryComp(uid, out TransformComponent? transform))
                PopupInternal(message, type, transform.Coordinates, uid, true, _timing.CurTick);
        }

        public override void PopupEntity(string? message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (_playerManager.LocalSession == recipient
                && TryComp(uid, out TransformComponent? transform))
                PopupInternal(message, type, transform.Coordinates, uid, true, _timing.CurTick);
        }

        public override void PopupEntity(string? recipientMessage, string? othersMessage, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (!TryComp(uid, out TransformComponent? transform))
                return;

            if (recipient != null && _playerManager.LocalEntity == recipient)
                PopupInternal(recipientMessage, type, transform.Coordinates, uid, true, _timing.CurTick);
            else
                PopupInternal(othersMessage, type, transform.Coordinates, uid, true, _timing.CurTick);
        }

        #endregion

        #region Network Event Handlers

        private void OnPopupCursorEvent(PopupCursorEvent ev)
        {
            PopupCursorInternal(ev.Message, ev.Type, false, ev.Tick);
        }

        private void OnPopupCoordinatesEvent(PopupCoordinatesEvent ev)
        {
            PopupInternal(ev.Message, ev.Type, GetCoordinates(ev.Coordinates), null, false, ev.Tick);
        }

        private void OnPopupEntityEvent(PopupEntityEvent ev)
        {
            var entity = GetEntity(ev.Uid);

            if (TryComp(entity, out TransformComponent? transform))
                PopupInternal(ev.Message, ev.Type, transform.Coordinates, entity, false, ev.Tick);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            _aliveCursorLabels.Clear();
            _aliveWorldLabels.Clear();
            _predictedPopups.Clear();
        }

        #endregion

        public static float GetPopupLifetime(PopupLabel label)
        {
            return Math.Clamp(PopupLifetimePerCharacter * label.Text.Length,
                MinimumPopupLifetime,
                MaximumPopupLifetime);
        }

        public override void FrameUpdate(float frameTime)
        {
            if (_aliveWorldLabels.Count == 0 && _aliveCursorLabels.Count == 0)
                return;

            if (_aliveWorldLabels.Count > 0)
            {
                var aliveWorldToRemove = new ValueList<WorldPopupData>();
                foreach (var (data, label) in _aliveWorldLabels)
                {
                    label.TotalTime += frameTime;
                    if (label.TotalTime > GetPopupLifetime(label) || Deleted(label.InitialPos.EntityId))
                    {
                        aliveWorldToRemove.Add(data);
                    }
                }
                foreach (var data in aliveWorldToRemove)
                {
                    _aliveWorldLabels.Remove(data);
                }
            }

            if (_aliveCursorLabels.Count > 0)
            {
                var aliveCursorToRemove = new ValueList<CursorPopupData>();
                foreach (var (data, label) in _aliveCursorLabels)
                {
                    label.TotalTime += frameTime;
                    if (label.TotalTime > GetPopupLifetime(label))
                    {
                        aliveCursorToRemove.Add(data);
                    }
                }
                foreach (var data in aliveCursorToRemove)
                {
                    _aliveCursorLabels.Remove(data);
                }
            }
        }

        public abstract class PopupLabel
        {
            public PopupType Type = PopupType.Small;
            public string Text { get; set; } = string.Empty;
            public float TotalTime { get; set; }
            public int Repeats = 1;
        }

        public sealed class WorldPopupLabel(EntityCoordinates coordinates) : PopupLabel
        {
            /// <summary>
            /// The original EntityCoordinates of the label.
            /// </summary>
            public EntityCoordinates InitialPos = coordinates;
        }

        public sealed class CursorPopupLabel(ScreenCoordinates screenCoords) : PopupLabel
        {
            public ScreenCoordinates InitialPos = screenCoords;
        }

        [UsedImplicitly]
        private record struct WorldPopupData(
            string Message,
            PopupType Type,
            EntityCoordinates Coordinates,
            EntityUid? Entity);

        [UsedImplicitly]
        private record struct CursorPopupData(
            string Message,
            PopupType Type);

        /// <summary>
        /// Used to uniquely identify popups to make sure we don't show popups networked from the server if we already predicted them.
        /// C# automatically implements GetHashCode() for record structs and each of the members here can be hashed.
        /// </remarks>
        [UsedImplicitly]
        private readonly record struct PopupHash(
            string Message,
            EntityUid? Uid,
            EntityCoordinates? Coordinates,
            PopupType Type,
            GameTick Tick);
    }
}
