using ECSAlpha.DOTS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECSAlpha.DOTS.ComponeSystems
{
    partial struct ObjectLifetimeDestroySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (objectLifetimeComponent, entity) in SystemAPI.Query<RefRO<ObjectLifetimeComponent>>().WithEntityAccess())
            {
                if (objectLifetimeComponent.ValueRO.TimeInit + objectLifetimeComponent.ValueRO.LifeTime < SystemAPI.Time.ElapsedTime)
                {
                    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                    ecb.DestroyEntity(entity);
                }
            }
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
