using ECSAlpha.DOTS.Tags;
using Unity.Burst;
using Unity.Entities;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial struct PlayerControllerInitialization : ISystem
    {
        private bool m_IsInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_IsInitialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (m_IsInitialized) return;

            foreach (var (pcTag, pcEntity) in SystemAPI.Query<RefRO<PlayerControllerTag>>().WithEntityAccess())
            {
                m_IsInitialized = true;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
