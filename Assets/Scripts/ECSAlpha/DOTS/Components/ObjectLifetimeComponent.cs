using Unity.Entities;

namespace ECSAlpha.DOTS.Components
{
    public struct ObjectLifetimeComponent : IComponentData
    {
        public double TimeInit;
        public int LifeTime;
    }
}