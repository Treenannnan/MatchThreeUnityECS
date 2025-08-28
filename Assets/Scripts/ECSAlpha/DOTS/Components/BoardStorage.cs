using Unity.Entities;

namespace ECSAlpha.DOTS.Components
{
    public struct BoardGemsBuffer : IBufferElementData
    {
        public Entity GemEntity;
        public int Index;
    }
}
