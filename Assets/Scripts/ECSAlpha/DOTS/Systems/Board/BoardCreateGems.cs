using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    partial struct BoardSpawner : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoardState>();
            state.RequireForUpdate<BoardData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (boardState, boardData, boardEntity) in SystemAPI.Query<RefRW<BoardState>, RefRO<BoardData>>().WithEntityAccess())
            {
                if (boardState.ValueRO.CurrentBoardState == EBoardState.Initialized)
                {
                    var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                    int totalGemsToSpawn = boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y;

                    var gemSpawnJob = new GemSpawnJob
                    {
                        BoardEntity = boardEntity,
                        GemPrefab = boardData.ValueRO.GemPrefab,
                        BoardSizeX = boardData.ValueRO.BoardSize.x,
                        BoardSizeY = boardData.ValueRO.BoardSize.y,
                        BoardGroundPosition = boardData.ValueRO.BoardGroundPosition,
                        ParallelWriter = ecb.AsParallelWriter()
                    };

                    var gemSpawnJobHandle = gemSpawnJob.Schedule(totalGemsToSpawn, 64, state.Dependency);

                    var setBoardStateJob = new SetBoardStateJob
                    {
                        BoardEntity = boardEntity,
                        BoardState = boardState.ValueRO,
                        ECB = ecb,
                        NewState = EBoardState.SetupGems
                    };

                    state.Dependency = setBoardStateJob.Schedule(gemSpawnJobHandle);
                }
            }
        }
    }

    [BurstCompile]
    public struct GemSpawnJob : IJobParallelFor
    {
        public Entity BoardEntity;
        public Entity GemPrefab;
        public int BoardSizeX;
        public int BoardSizeY;
        public float BoardGroundPosition;
        public EntityCommandBuffer.ParallelWriter ParallelWriter;

        public void Execute(int index)
        {
            int x = index % BoardSizeX;
            int y = index / BoardSizeX;

            Entity newEntity = ParallelWriter.Instantiate(index, GemPrefab);

            ParallelWriter.SetComponent(index, newEntity,
                LocalTransform.FromPosition(new float3(
                    x - (BoardSizeX - 1) / 2.0f, y + BoardGroundPosition, 0)));

            ParallelWriter.SetComponent(index, newEntity, new GemState
            {
                GemPosition = index,
                CurrentState = EGemState.None,
                IsSelected = false
            });

            ParallelWriter.AppendToBuffer(index, BoardEntity, new BoardGemsBuffer
            {
                GemEntity = newEntity,
                Index = index
            });
        }

    }
}
