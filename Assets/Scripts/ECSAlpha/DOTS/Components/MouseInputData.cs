using Unity.Entities;
using Unity.Mathematics;

namespace ECSAlpha.DOTS.Components
{
    public struct MouseInputDataBuffer : IBufferElementData
    {
        public float2 MouseWorldPosition;
        public bool IsPress;
    }
}