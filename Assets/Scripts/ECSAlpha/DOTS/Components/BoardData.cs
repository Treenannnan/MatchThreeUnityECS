using Unity.Entities;
using Unity.Mathematics;

namespace ECSAlpha.DOTS.Components
{
    public struct BoardData : IComponentData
    {
        public int2 BoardSize;
        public Entity GemPrefab;
        public Entity BrokenGlessPrefab;
        public float BoardGroundPosition;
        public float BoardDeepPosition;
        public int PlayArea;
    }
}
