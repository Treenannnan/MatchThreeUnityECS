using ECSAlpha.DOTS.Tags;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSAlpha.DOTS.Components
{
    public enum EGemState : uint
    {
        None = 0,
        Idel,
        Falling,
        Destroy,
        Moving
    }

    public struct GemState : IComponentData
    {
        public int GemPosition;
        public EGemState CurrentState;
        public bool IsSelected;
        public EGemColorTags ColorTag;
    }

    public struct GemTransformState : IComponentData
    {
        public float3 PositionTarget;
        public float3 RotationTarget;
    }
}