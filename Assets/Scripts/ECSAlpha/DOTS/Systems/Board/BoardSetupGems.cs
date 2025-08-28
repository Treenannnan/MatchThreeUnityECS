using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Tags;
using ECSAlpha.Hepers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace ECSAlpha.DOTS.Systems.Board
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    partial struct BoardSetupGems : ISystem
    {
        private BufferLookup<BoardGemsBuffer> m_BoardGemsBufferLookup;
        private BufferLookup<Child> m_ChildLookup;
        private ComponentLookup<URPMaterialPropertyBaseColor> m_URPMaterialPropertyBaseColorLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ChildLookup = state.GetBufferLookup<Child>(isReadOnly: true);
            m_URPMaterialPropertyBaseColorLookup = state.GetComponentLookup<URPMaterialPropertyBaseColor>(isReadOnly: true);
            m_BoardGemsBufferLookup = state.GetBufferLookup<BoardGemsBuffer>(isReadOnly: false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (boardData, boardState, boardEntity) in SystemAPI.Query< RefRO<BoardData> , RefRW <BoardState>>().WithEntityAccess())
            {
                if (boardState.ValueRO.CurrentBoardState == EBoardState.SetupGems)
                {
                    m_BoardGemsBufferLookup.Update(ref state);
                    m_ChildLookup.Update(ref state);
                    m_URPMaterialPropertyBaseColorLookup.Update(ref state);

                    var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                    var randomGenerator = new Random(5236);

                    var gemCount = boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y;

                    var gemColors = new NativeArray<EGemColorTags>(gemCount, Allocator.TempJob);

                    for (int i = gemCount - 1; i >= 0; --i)
                    {
                        gemColors[i] = GemsHelper.GetColorByNumber(randomGenerator.NextInt(0, GemsHelper.MAX_GEM_COLOR_TAGS));
                    }

                    var gemsRandomColorJob = new GemsRandomColorJob
                    {
                        BoardEntity = boardEntity,
                        GemColors = gemColors,
                        ChildLookup = m_ChildLookup,
                        BoardGemsBufferLookup = m_BoardGemsBufferLookup,
                        URPMaterialPropertyBaseColorLookup = m_URPMaterialPropertyBaseColorLookup,
                        ParallelWriter = ecb.AsParallelWriter(),
                    };

                    var query = SystemAPI.QueryBuilder().WithAll<GemState, Child>().Build();
                    var gemsRandomColorJobHandle = gemsRandomColorJob.ScheduleParallel(query, state.Dependency);

                    state.Dependency = gemColors.Dispose(gemsRandomColorJobHandle);

                    boardState.ValueRW.CurrentBoardState = EBoardState.GameRunning;
                    boardState.ValueRW.CurrentGemState = EBoardGemState.Idle;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct GemsRandomColorJob : IJobEntity
    {
        public Entity BoardEntity;
        public NativeArray<EGemColorTags> GemColors;

        [NativeDisableContainerSafetyRestriction] public BufferLookup<BoardGemsBuffer> BoardGemsBufferLookup;
        [ReadOnly] public BufferLookup<Child> ChildLookup;
        [ReadOnly] public ComponentLookup<URPMaterialPropertyBaseColor> URPMaterialPropertyBaseColorLookup;

        public EntityCommandBuffer.ParallelWriter ParallelWriter;

        public void Execute(Entity gemEntity, [EntityIndexInQuery] int sortKey, ref GemState gemState)
        {
            if (!ChildLookup.HasBuffer(gemEntity))
                return;

            var children = ChildLookup[gemEntity];

            foreach (var childElement in children)
            {
                var childEntity = childElement.Value;

                if (!URPMaterialPropertyBaseColorLookup.HasComponent(childEntity))
                    continue;

                var collorTag = GemColors[sortKey];

                ParallelWriter.SetComponent(sortKey, childEntity, new URPMaterialPropertyBaseColor { Value = collorTag.ColorValue() });
                ParallelWriter.SetName(sortKey, gemEntity, $"Gem_{gemState.GemPosition}");

                var updateGemState = gemState;

                updateGemState.IsSelected = false;
                updateGemState.CurrentState = EGemState.Idel;
                updateGemState.ColorTag = collorTag;

                ParallelWriter.SetComponent(sortKey, gemEntity, updateGemState);
            }
        }
    }
}