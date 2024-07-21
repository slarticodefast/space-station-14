using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Crayon
{
    [NetworkedComponent, ComponentProtoName("Crayon"), Access(typeof(SharedCrayonSystem))]
    public abstract partial class SharedCrayonComponent : Component
    {
        public string SelectedState { get; set; } = string.Empty;

        [DataField]
        public Color Color;

        [DataField]
        public Angle Angle;

        [Serializable, NetSerializable]
        public enum CrayonUiKey : byte
        {
            Key,
        }
    }

    [Serializable, NetSerializable]
    public sealed class CrayonSelectMessage : BoundUserInterfaceMessage
    {
        public readonly string State;
        public CrayonSelectMessage(string selected)
        {
            State = selected;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CrayonColorMessage : BoundUserInterfaceMessage
    {
        public readonly Color Color;
        public CrayonColorMessage(Color color)
        {
            Color = color;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CrayonAngleMessage : BoundUserInterfaceMessage
    {
        public readonly Angle Angle;
        public CrayonAngleMessage(Angle angle)
        {
            Angle = angle;
        }
    }

    [Serializable, NetSerializable]
    public enum CrayonVisuals
    {
        State,
        Color
    }

    [Serializable, NetSerializable]
    public sealed class CrayonComponentState : ComponentState
    {
        public readonly Color Color;
        public readonly string State;
        public readonly int Charges;
        public readonly int Capacity;

        public CrayonComponentState(Color color, string state, int charges, int capacity)
        {
            Color = color;
            State = state;
            Charges = charges;
            Capacity = capacity;
        }
    }
    [Serializable, NetSerializable]
    public sealed class CrayonBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string Selected;
        public bool SelectableColor;
        public Color Color;
        public Angle Angle;

        public CrayonBoundUserInterfaceState(string selected, bool selectableColor, Color color, Angle angle)
        {
            Selected = selected;
            SelectableColor = selectableColor;
            Color = color;
            Angle = angle;
        }
    }
}
