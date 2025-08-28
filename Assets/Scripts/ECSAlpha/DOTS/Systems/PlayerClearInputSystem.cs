using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Tags;
using Unity.Burst;
using Unity.Entities;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(InputClearSystemGroup))]
    partial struct PlayerClearInputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerControllerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerTag, entity) in SystemAPI.Query<RefRO<PlayerControllerTag>>().WithEntityAccess())
            {
                var inputBuffer = state.EntityManager.GetBuffer<MouseInputDataBuffer>(entity);

                inputBuffer.Clear();
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}