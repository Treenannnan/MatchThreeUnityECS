using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Jobs;
using ECSAlpha.DOTS.Tags;
using ECSAlpha.Hepers;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(PhysicsSimulateSystemGroup))]
    partial struct GemFallingDownSystem : ISystem
    {
        private ComponentLookup<BoardData> m_BoardDataLookup;
        private BufferLookup<BoardGemsBuffer> m_BoardGemsBufferLookup;
        private ComponentLookup<GemState> m_GemStateLookup;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_BoardDataLookup = state.GetComponentLookup<BoardData>(isReadOnly: true);
            m_BoardGemsBufferLookup = state.GetBufferLookup<BoardGemsBuffer>(isReadOnly: false);
            m_GemStateLookup = state.GetComponentLookup<GemState>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (boardTag, boardData, boardState, boardEntity) in SystemAPI.Query<RefRO<BoardTag>, RefRO<BoardData>, RefRO<BoardState>>().WithEntityAccess())
            {
                if (boardState.ValueRO.CurrentGemState != EBoardGemState.Move)
                    return;

                m_BoardDataLookup.Update(ref state);
                m_BoardGemsBufferLookup.Update(ref state);
                m_GemStateLookup.Update(ref state);

                var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                var gemStates = new NativeList<GemState>(boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y, Allocator.TempJob);

                var boardGemsBufferArray = new NativeArray<BoardGemsBuffer>(boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y, Allocator.TempJob);

                var boardGemsBuffer = m_BoardGemsBufferLookup[boardEntity];

                for (int i = 0; i < boardGemsBuffer.Length; i++)
                {
                    boardGemsBufferArray[i] = boardGemsBuffer[i];
                }

                var boardGemsBufferUpdate = new NativeList<BoardGemsBuffer>(boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y, Allocator.TempJob);

                var gemsFallingJob = new GemsFallingJob
                {
                    BoardEntity = boardEntity,
                    FallingSpeed = 8f,
                    DeltaTime = SystemAPI.Time.DeltaTime,
                    BoardDataLookup = m_BoardDataLookup,
                    BoardGemsBuffer = boardGemsBufferArray,
                    BoardGemsBufferUpdate = boardGemsBufferUpdate.AsParallelWriter(),
                    ParallelWriter = ecb.AsParallelWriter(),
                    GemStatesParallelWriter = gemStates.AsParallelWriter(),
                };

                var query = SystemAPI.QueryBuilder().WithAll<GemState, GemTransformState, LocalTransform>().Build();

                var gemsFallingJobHandle = gemsFallingJob.ScheduleParallel(query, state.Dependency);

                var boardGemsBufferUpdateJob = new BoardGemsBufferUpdateJob
                {
                    BoardGemsBufferUpdate = boardGemsBufferUpdate,
                };

                var boardGemsBufferUpdateJobHandle = boardGemsBufferUpdateJob.Schedule( gemsFallingJobHandle);

                var setBoardStateJob = new SetBoardGemStateWithBoardGemsBufferJob
                {
                    BoardEntity = boardEntity,
                    BoardState = boardState.ValueRO,
                    GemStates = gemStates,
                    ECB = ecb,
                    NewState = EBoardGemState.Check,
                };

                var setBoardStateJobHandle = setBoardStateJob.Schedule(boardGemsBufferUpdateJobHandle);
                gemStates.Dispose(setBoardStateJobHandle);
                boardGemsBufferArray.Dispose(setBoardStateJobHandle);
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
    public partial struct GemsFallingJob : IJobEntity
    {
        public Entity BoardEntity;
        public float FallingSpeed;
        public float DeltaTime;

        [ReadOnly]
        public ComponentLookup<BoardData> BoardDataLookup;

        public NativeArray<BoardGemsBuffer> BoardGemsBuffer;
        public NativeList<BoardGemsBuffer>.ParallelWriter BoardGemsBufferUpdate;

        public EntityCommandBuffer.ParallelWriter ParallelWriter;
        public NativeList<GemState>.ParallelWriter GemStatesParallelWriter;

        public void Execute(Entity gemEntity, [EntityIndexInQuery] int sortKey, ref GemState gemState, ref GemTransformState gemTransformState, ref LocalTransform localTransform)
        {
            if (!BoardDataLookup.HasComponent(BoardEntity)) return;

            var boardData = BoardDataLookup.GetRefRO(BoardEntity);

            var neighbors = boardData.ValueRO.GetNeighbors(gemState);
            int targetGridY = neighbors.w;

            bool canFall = false;

            if (targetGridY != -1)
            {
                if (BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(targetGridY).GemEntity == Entity.Null)
                {
                    canFall = true;
                }
            }

            if (canFall)
            {
                var targetWorldPos = boardData.ValueRO.GetLocalPositionFromGemPosistion(targetGridY);

                var newLocalPosition = localTransform.Position;
                newLocalPosition.y -= DeltaTime * FallingSpeed;
                
                if(BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(gemState.GemPosition).GemEntity != null 
                    && BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(gemState.GemPosition).GemEntity == gemEntity)
                {

                    BoardGemsBufferUpdate.AddNoResize(new BoardGemsBuffer
                    {
                        GemEntity = Entity.Null,
                        Index = gemState.GemPosition
                    });
                    
                }
                
                if (newLocalPosition.y <= targetWorldPos.y)
                {
                    var updatedGemState = gemState;
                    updatedGemState.GemPosition = neighbors.w;

                    neighbors = boardData.ValueRO.GetNeighbors(updatedGemState);

                    if (neighbors.w != -1 && BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(neighbors.w).GemEntity == Entity.Null)
                        updatedGemState.CurrentState = EGemState.Falling;
                    else
                        updatedGemState.CurrentState = EGemState.Idel;

                    BoardGemsBufferUpdate.AddNoResize(new BoardGemsBuffer
                    {
                        GemEntity = gemEntity,
                        Index = updatedGemState.GemPosition
                    });

                    ParallelWriter.SetComponent(sortKey, gemEntity, updatedGemState);
                    ParallelWriter.SetName(sortKey, gemEntity, $"Gem_{updatedGemState.GemPosition}");
                    ParallelWriter.SetComponent(sortKey, gemEntity, LocalTransform.FromPosition(targetWorldPos));
                    GemStatesParallelWriter.AddNoResize(updatedGemState);
                }
                else
                {
                    if (gemState.CurrentState != EGemState.Falling)
                    {
                        var updatedGemState = gemState;
                        updatedGemState.CurrentState = EGemState.Falling;
                        ParallelWriter.SetComponent(sortKey, gemEntity, updatedGemState);
                        GemStatesParallelWriter.AddNoResize(updatedGemState);
                    }
                    else
                        GemStatesParallelWriter.AddNoResize(gemState);

                    ParallelWriter.SetComponent(sortKey, gemEntity, LocalTransform.FromPosition(newLocalPosition));
                }
            }
            else
            {
                if (gemState.CurrentState != EGemState.Idel)
                {
                    var updatedGemState = gemState;
                    updatedGemState.CurrentState = EGemState.Idel;
                    ParallelWriter.SetComponent(sortKey, gemEntity, updatedGemState);
                    GemStatesParallelWriter.AddNoResize(updatedGemState);
                }
                else
                {
                    GemStatesParallelWriter.AddNoResize(gemState);
                }
            }
        }
    }
}
