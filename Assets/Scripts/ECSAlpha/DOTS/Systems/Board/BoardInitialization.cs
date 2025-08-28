using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Jobs;
using ECSAlpha.Hepers;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ECSAlpha.DOTS.Systems.Board
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    partial struct BoardInitialization : ISystem
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

            foreach (var (boardState, boardView, boardEntity) in SystemAPI.Query<RefRW<BoardState>, RefRO<BoardView>>().WithEntityAccess())
            {
                if (boardState.ValueRO.CurrentBoardState == EBoardState.None)
                {
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                    var boardInitializeJob = new BoardInitializeJob
                    {
                        BoardEntity = boardEntity,
                        ParallelWriter = ecb,
                    };

                    var boardInitializeJobHandle = boardInitializeJob.Schedule(state.Dependency);

                    var cannonSetupJob = new CannonSetupJob
                    {
                        Seed = 9856,
                        ParallelWriter = ecb,
                    };

                    var cannonSetupJobHandle = cannonSetupJob.Schedule(boardInitializeJobHandle);

                    var setBoardStateJob = new SetBoardStateJob
                    {
                        BoardEntity = boardEntity,
                        BoardState = boardState.ValueRO,
                        ECB = ecb,
                        NewState = EBoardState.Initialized,
                    };

                    state.Dependency = setBoardStateJob.Schedule(cannonSetupJobHandle);

                    m_IsInitialized = true;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

    }

    public partial struct BoardInitializeJob : IJobEntity
    {
        public Entity BoardEntity;
        public EntityCommandBuffer ParallelWriter;

        public void Execute(Entity gemEntity, [EntityIndexInQuery] int sortKey, in PlayerControllerData pcData)
        {
            var pcDataUpdate = pcData;
            pcDataUpdate.BoardEntity = BoardEntity;
            ParallelWriter.SetComponent(gemEntity, pcDataUpdate);
        }
    }

    [BurstCompile]
    public partial struct CannonSetupJob : IJobEntity
    {
        public uint Seed;
        public EntityCommandBuffer ParallelWriter;
        public void Execute(Entity cannonEntity, [EntityIndexInQuery] int sortKey, in CannonData cannonData, in CannonState cannonState)
        {
            var updateCannonState = cannonState;

            var randomGenerator = new Random(Seed);

            updateCannonState.CurrentBulletColor = GemsHelper.GetColorByNumber(randomGenerator.NextInt(0, GemsHelper.MAX_GEM_COLOR_TAGS));

            updateCannonState.NextBulletColor = GemsHelper.GetColorByNumber(randomGenerator.NextInt(0, GemsHelper.MAX_GEM_COLOR_TAGS));
            ParallelWriter.SetComponent(cannonEntity, updateCannonState);
        }
    }
}
