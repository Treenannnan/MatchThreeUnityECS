using ECSAlpha.DOTS.Tags;
using Unity.Entities;

namespace ECSAlpha.DOTS.Components
{
    public struct CannonData : IComponentData
    {
        public int MaxBullet;
    }

    public struct CannonState : IComponentData
    {
        public EGemColorTags CurrentBulletColor;
        public EGemColorTags NextBulletColor;
        public int CurrentBulletAmount;
    }
}