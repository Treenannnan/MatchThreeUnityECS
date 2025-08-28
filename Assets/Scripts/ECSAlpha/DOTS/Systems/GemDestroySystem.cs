using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Jobs;
using ECSAlpha.DOTS.Tags;
using ECSAlpha.Hepers;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.UIElements;

namespace ECSAlpha.DOTS.Systems
{
    partial struct GemDestroySystem : ISystem
    {
        private BufferLookup<BoardGemsBuffer> m_BoardGemsBufferLookup;
        private BufferLookup<Child> m_ChildLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ChildLookup = state.GetBufferLookup<Child>(isReadOnly: true);
            m_BoardGemsBufferLookup = state.GetBufferLookup<BoardGemsBuffer>(isReadOnly: false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (boardTag, boardData, boardState, boardEntity) in SystemAPI.Query<RefRO<BoardTag>, RefRO<BoardData>, RefRO <BoardState>>().WithEntityAccess())
            {
                if (boardState.ValueRO.CurrentGemState != EBoardGemState.Destroy)
                    return;

                m_BoardGemsBufferLookup.Update(ref state);
                m_ChildLookup.Update(ref state);

                var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                var boardGemsBufferUpdate = new NativeList<BoardGemsBuffer>(boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y, Allocator.TempJob);

                var gemsDestroyJob = new GemsDestroyJob
                {
                    BoardEntity = boardEntity,
                    BoardTime = SystemAPI.Time.ElapsedTime,
                    ChildLookup = m_ChildLookup,
                    BoardGemsBufferLookup = m_BoardGemsBufferLookup,
                    BoardData = boardData.ValueRO,
                    ECB = ecb.AsParallelWriter(),
                    BoardGemsBufferUpdate = boardGemsBufferUpdate.AsParallelWriter()
                };

                var query = SystemAPI.QueryBuilder().WithAll<GemData, LocalTransform, GemState>().Build();
                var gemsDestroyJobHandle = gemsDestroyJob.ScheduleParallel(query, state.Dependency);

                var boardGemsBufferUpdateJob = new BoardGemsBufferUpdateJob
                {
                    BoardGemsBufferUpdate = boardGemsBufferUpdate,
                };

                var boardGemsBufferUpdateJobHandle = boardGemsBufferUpdateJob.Schedule(gemsDestroyJobHandle);

                var setBoardStateJob = new SetBoardGemStateJob
                {
                    BoardEntity = boardEntity,
                    BoardState = boardState.ValueRO,
                    ECB = ecb,
                    NewState = EBoardGemState.Move,
                };

                var setBoardStateJobHandle = setBoardStateJob.Schedule(boardGemsBufferUpdateJobHandle);

                boardGemsBufferUpdate.Dispose(setBoardStateJobHandle);

                state.Dependency = setBoardStateJobHandle;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct GemsDestroyJob : IJobEntity
    {
        public Entity BoardEntity;
        public double BoardTime;
        [NativeDisableContainerSafetyRestriction] public BufferLookup<BoardGemsBuffer> BoardGemsBufferLookup;
        [ReadOnly] public BufferLookup<Child> ChildLookup;
        [ReadOnly] public BoardData BoardData;

        public EntityCommandBuffer.ParallelWriter ECB;
        public NativeList<BoardGemsBuffer>.ParallelWriter BoardGemsBufferUpdate;

        public void Execute(Entity gemEntity, [EntityIndexInQuery] int sortKey, in GemData gemData, in LocalTransform localTransform, in GemState gemState)
        {
            /*
            if (!ChildLookup.HasBuffer(gemEntity))
                return;

            var children = ChildLookup[gemEntity];
            var gemIndex = gemState.GemPosition;
            
            foreach (var childElement in children)
            {
                var childEntity = childElement.Value;

                if (!URPMaterialPropertyBaseColorLookup.HasComponent(childEntity))
                    continue;

                if (gemState.CurrentState == EGemState.Destroy)
                {
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
                    
                    ECB.SetComponent(sortKey, gemEntity, LocalTransform.FromPosition(0, -1000, 0));

                    var updateGemState = gemState;
                    updateGemState.IsSelected = false;
                    updateGemState.GemPosition = -1;
                    updateGemState.ColorTag = EGemColorTags.None;

                    ECB.SetComponent(sortKey, gemEntity, updateGemState);
                }
            }
            */


            if (gemState.CurrentState == EGemState.Destroy)
            {
                var boardBuffer = BoardGemsBufferLookup[BoardEntity];

                if (gemState.GemPosition >= 0 && gemState.GemPosition < boardBuffer.Length)
                {
                    BoardGemsBufferUpdate.AddNoResize(new BoardGemsBuffer
                    {
                        GemEntity = Entity.Null,
                        Index = gemState.GemPosition
                    });
                }

                for(var x = 0; x < 4; ++x)
                {
                    for (var y = 0; y < 4; ++y)
                    {
                        Entity brokenEntity = ECB.Instantiate(sortKey, BoardData.BrokenGlessPrefab);

                        var newPos = localTransform.Position;
                        newPos.x -= 0.5f;
                        newPos.y -= 0.5f;
                        newPos.z += 3;
                        newPos.x += (float)x / 3;
                        newPos.y += (float)y / 3;

                        ECB.SetComponent(sortKey, brokenEntity, LocalTransform.FromPosition(new float3(newPos)));
                        ECB.AddComponent(sortKey, brokenEntity, new ObjectLifetimeComponent
                        {
                            TimeInit = BoardTime,
                            LifeTime = 5
                        });
                    }
                }

                /*
                if (ChildLookup.HasBuffer(brokenEntity))
                {
                    var children = ChildLookup[gemData.BrokenGress];
                    foreach (var childElementA in children)
                    {
                        ECB.SetComponent(sortKey, childElementA.Value, LocalTransform.FromPosition(localTransform.Position));
                    }
                }
                */
                /*
                ECB.SetComponent(sortKey, gemData.BrokenGress, LocalTransform.FromPosition(localTransform.Position));

                
                ECB.RemoveComponent<Disabled>(sortKey, gemData.BrokenGress);

                if (ChildLookup.HasBuffer(gemData.BrokenGress))
                {
                    var children = ChildLookup[gemData.BrokenGress];
                }
                */
                /*
                foreach (var childElementA in children)
                {
                    ECB.SetComponent(sortKey, childElementA.Value, LocalTransform.FromPosition(localTransform.Position));

                    //ECB.RemoveComponent<Disabled>(sortKey, childElementA.Value);
                    /*
                    foreach (var childElementB in childrenA)
                    {
                        var childrenB = ChildLookup[childElementB.Value];

                        ECB.SetComponent(sortKey, childElementB.Value, LocalTransform.FromPosition(localTransform.Position));

                        ECB.RemoveComponent<Disabled>(sortKey, childElementB.Value);
                    }
                    
                }
            */
                /*
                ECB.SetComponent(sortKey, gemEntity, LocalTransform.FromPosition(0, -1000, 0));

                var updateGemState = gemState;
                updateGemState.IsSelected = false;
                updateGemState.GemPosition = -1;
                updateGemState.ColorTag = EGemColorTags.None;
                updateGemState.CurrentState = EGemState.None;

                ECB.SetComponent(sortKey, gemEntity, updateGemState);
                */

                ECB.DestroyEntity(sortKey, gemEntity);
            }
        }
    }
}