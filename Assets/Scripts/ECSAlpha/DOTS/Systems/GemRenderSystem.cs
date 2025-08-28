using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Jobs;
using ECSAlpha.DOTS.Tags;
using ECSAlpha.Hepers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(RanderSystemGroup))]
    partial struct GemRenderSystem : ISystem
    {
        private BufferLookup<BoardGemsBuffer> m_BoardGemsBufferLookup;
        private BufferLookup<Child> m_ChildLookup;
        private ComponentLookup<URPMaterialPropertyBaseColor> m_URPMaterialPropertyBaseColorLookup;

        private uint m_CannonSeed;
        private Random m_RandomGenerator;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ChildLookup = state.GetBufferLookup<Child>(isReadOnly: true);
            m_URPMaterialPropertyBaseColorLookup = state.GetComponentLookup<URPMaterialPropertyBaseColor>(isReadOnly: true);
            m_BoardGemsBufferLookup = state.GetBufferLookup<BoardGemsBuffer>(isReadOnly: false);

            m_CannonSeed = 85249;

            m_RandomGenerator = new Random(m_CannonSeed);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (boardTag, boardState, boardEntity) in SystemAPI.Query<RefRO<BoardTag>, RefRO<BoardState>>().WithEntityAccess())
            {
                if (boardState.ValueRO.CurrentGemState != EBoardGemState.Select)
                    return;

                m_BoardGemsBufferLookup.Update(ref state);
                m_ChildLookup.Update(ref state);
                m_URPMaterialPropertyBaseColorLookup.Update(ref state);

                var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                var fireColor = new NativeList<EGemColorTags>(1, Allocator.TempJob);

                var cannonFireJob = new CannonFireJob
                {
                    NewColor = GemsHelper.GetColorByNumber(m_RandomGenerator.NextInt(0, GemsHelper.MAX_GEM_COLOR_TAGS)),
                    FireColor = fireColor.AsParallelWriter(),
                };

                var cannonFireJobHandle = cannonFireJob.Schedule(state.Dependency);
                
                var gemsChangeColorJob = new GemsChangeColorJob
                {
                    BoardEntity = boardEntity,
                    ChildLookup = m_ChildLookup,
                    BoardGemsBufferLookup= m_BoardGemsBufferLookup,
                    URPMaterialPropertyBaseColorLookup = m_URPMaterialPropertyBaseColorLookup,
                    ECB = ecb.AsParallelWriter(),
                    NewColor = fireColor
                };

                var gemsChangeColorJobHandle = gemsChangeColorJob.ScheduleParallel(cannonFireJobHandle);
                
                var setBoardStateJob = new SetBoardGemStateJob
                {
                    BoardEntity = boardEntity,
                    BoardState = boardState.ValueRO,
                    ECB = ecb,
                    NewState = EBoardGemState.Check,
                };

                var setBoardStateJobHandle = setBoardStateJob.Schedule(gemsChangeColorJobHandle);

                fireColor.Dispose(setBoardStateJobHandle);

                state.Dependency = setBoardStateJobHandle;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct CannonFireJob : IJobEntity
    {
        public EGemColorTags NewColor;
        public NativeList<EGemColorTags>.ParallelWriter FireColor;
        public void Execute(Entity cannonEntity, [EntityIndexInQuery] int sortKey, in CannonData cannonData, ref CannonState cannonState)
        {
            if (cannonState.CurrentBulletAmount <= 0)
            {
                FireColor.AddNoResize(EGemColorTags.None);
                return;
            }
            
            FireColor.AddNoResize(cannonState.CurrentBulletColor);

            var updateCannonState = cannonState;

            updateCannonState.CurrentBulletColor = updateCannonState.NextBulletColor;
            updateCannonState.CurrentBulletAmount -= 1;
            updateCannonState.NextBulletColor = NewColor;

            cannonState = updateCannonState;

        }
    }

    [BurstCompile]
    public partial struct GemsChangeColorJob : IJobEntity
    {
        public Entity BoardEntity;
        [NativeDisableContainerSafetyRestriction] public BufferLookup<BoardGemsBuffer> BoardGemsBufferLookup;
        [ReadOnly] public BufferLookup<Child> ChildLookup;
        [ReadOnly] public ComponentLookup<URPMaterialPropertyBaseColor> URPMaterialPropertyBaseColorLookup;

        public EntityCommandBuffer.ParallelWriter ECB;

        [ReadOnly]
        public NativeList<EGemColorTags> NewColor;

        public void Execute(Entity gemEntity, [EntityIndexInQuery] int sortKey, ref GemState gemState, ref LocalTransform localTransform)
        {
            if (!ChildLookup.HasBuffer(gemEntity))
                return;

            var children = ChildLookup[gemEntity];
            var gemIndex = gemState.GemPosition;
            foreach (var childElement in children)
            {
                var childEntity = childElement.Value;

                if (!URPMaterialPropertyBaseColorLookup.HasComponent(childEntity))
                    continue;

                //ECB.SetComponent(sortKey, childEntity, new URPMaterialPropertyBaseColor { Value = gemState.IsSelected ? NewColor : new float4(1, 1, 1, 1) });

                if (gemState.IsSelected)
                {
                    /*
                    var boardBuffer = BoardGemsBufferLookup[BoardEntity];

                    if (gemState.GemPosition >= 0 && gemState.GemPosition < boardBuffer.Length)
                    {
                        BoardGemsBuffer emptyGemEntry = new BoardGemsBuffer
                        {
                            GemEntity = Entity.Null,
                            Index = gemState.GemPosition
                        };

                        boardBuffer[gemState.GemPosition] = emptyGemEntry;
                    }
                    
                    //ECB.SetComponent(sortKey, gemEntity, LocalTransform.FromPosition(0, -1000, 0));
                    */
                    var updateGemState = gemState;
                    updateGemState.IsSelected = false;
                    updateGemState.ColorTag = NewColor[0];

                    ECB.SetComponent(sortKey, childEntity, new URPMaterialPropertyBaseColor { Value = updateGemState.ColorTag.ColorValue() });
                    ECB.SetComponent(sortKey, gemEntity, updateGemState);
                }
            }
        }
    }
}
