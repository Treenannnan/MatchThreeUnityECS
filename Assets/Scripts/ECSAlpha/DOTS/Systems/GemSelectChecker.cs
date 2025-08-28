using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Jobs;
using ECSAlpha.DOTS.Tags;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(InputSimulateSystemGroup))]
    partial struct GemSelectChecker : ISystem
    {
        private ComponentTypeHandle<GemState> m_GemStateTypeHandle;
        private ComponentTypeHandle<LocalTransform> m_LocalTransformTypeHandle;

        private EntityQuery m_QueryForChecker;
        private EntityQuery m_QueryForDeselect;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_GemStateTypeHandle = state.GetComponentTypeHandle<GemState>(false);
            m_LocalTransformTypeHandle = state.GetComponentTypeHandle<LocalTransform>(true);

            m_QueryForChecker = SystemAPI.QueryBuilder().WithAll<GemState, LocalTransform>().Build();
            m_QueryForDeselect = SystemAPI.QueryBuilder().WithAll<GemState>().Build();

            state.RequireForUpdate<BoardState>();
            state.RequireForUpdate<PlayerControllerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var boardState = SystemAPI.GetSingleton<BoardState>();

            if (boardState.CurrentBoardState != EBoardState.GameRunning)
                return;

            if (boardState.CurrentGemState != EBoardGemState.Idle)
                return;

            foreach (var (playerTag, playerData, entity) in SystemAPI.Query<RefRO<PlayerControllerTag>, RefRO<PlayerControllerData>>().WithEntityAccess())
            {
                var inputBuffer = state.EntityManager.GetBuffer<MouseInputDataBuffer>(entity);

                if (inputBuffer.IsEmpty)
                {
                    continue;
                }

                m_GemStateTypeHandle.Update(ref state);
                m_LocalTransformTypeHandle.Update(ref state);

                foreach (var mouseEvent in inputBuffer)
                {
                    if (mouseEvent.IsPress)
                    {
                        var selectJob = new GemsSelectCheckerJob
                        {
                            MouseClickWorldPosition = new float3(mouseEvent.MouseWorldPosition.x, mouseEvent.MouseWorldPosition.y, 0),
                            GemStateTypeHandle = m_GemStateTypeHandle,
                            LocalTransformTypeHandle = m_LocalTransformTypeHandle
                        };

                        var selectJobHandle = selectJob.ScheduleParallel(m_QueryForChecker, state.Dependency);

                        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                        var setBoardStateJob = new SetBoardGemStateJob
                        {
                            BoardEntity = playerData.ValueRO.BoardEntity,
                            BoardState = boardState,
                            ECB = ecb,
                            NewState = EBoardGemState.Select,
                        };

                        state.Dependency = setBoardStateJob.Schedule(selectJobHandle);
                    }
                    else
                    {
                        var deselectJob = new DeselectGemsJob();
                        state.Dependency = deselectJob.ScheduleParallel(m_QueryForDeselect, state.Dependency);
                    }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct DeselectGemsJob : IJobEntity
    {
        public void Execute(ref GemState gemState)
        {
            gemState.IsSelected = false;
        }
    }

    [BurstCompile]
    public struct GemsSelectCheckerJob : IJobChunk
    {
        public float3 MouseClickWorldPosition;

        public ComponentTypeHandle<GemState> GemStateTypeHandle;

        [ReadOnly] public ComponentTypeHandle<LocalTransform> LocalTransformTypeHandle;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var gemStates = chunk.GetNativeArray(ref GemStateTypeHandle);
            var localTransform = chunk.GetNativeArray(ref LocalTransformTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var pos = localTransform[i].Position;

                bool isMouseOverGem = MouseClickWorldPosition.x > (pos.x - 0.5f) &&
                                      MouseClickWorldPosition.x < (pos.x + 0.5f) &&
                                      MouseClickWorldPosition.y > (pos.y - 0.5f) &&
                                      MouseClickWorldPosition.y < (pos.y + 0.5f);

                var currentGemState = gemStates[i];
                currentGemState.IsSelected = isMouseOverGem;
                gemStates[i] = currentGemState;
            }
        }
    }
}
