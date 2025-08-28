using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Jobs;
using ECSAlpha.DOTS.Tags;
using ECSAlpha.Hepers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(GameplayCalculateSystemGroup))]
    partial struct GemCheckMatchSystem : ISystem
    {
        private ComponentLookup<BoardData> m_BoardDataLookup;
        private BufferLookup<BoardGemsBuffer> m_BoardGemsBufferLookup;
        private ComponentLookup<GemState> m_GemStateLookup;

        private bool m_Checked;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_BoardDataLookup = state.GetComponentLookup<BoardData>(isReadOnly: true);
            m_BoardGemsBufferLookup = state.GetBufferLookup<BoardGemsBuffer>(isReadOnly: true);
            m_GemStateLookup = state.GetComponentLookup<GemState>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (boardTag, boardData, boardState, boardEntity) in SystemAPI.Query<RefRO<BoardTag>, RefRO<BoardData>, RefRW<BoardState>>().WithEntityAccess())
            {
                if (boardState.ValueRO.CurrentGemState != EBoardGemState.Check)
                {
                    m_Checked = false;
                    continue;
                }

                if (m_Checked)
                    continue;

                m_Checked = true;

                m_BoardDataLookup.Update(ref state);
                m_BoardGemsBufferLookup.Update(ref state);
                m_GemStateLookup.Update(ref state);

                var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                var gemStates = new NativeList<GemState>(boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y, Allocator.TempJob);

                var boardGemsBufferArray = new NativeArray<BoardGemsBuffer>(boardData.ValueRO.BoardSize.x * boardData.ValueRO.BoardSize.y, Allocator.TempJob);

                var boardGemsBuffer = m_BoardGemsBufferLookup[boardEntity];

                for(int i = 0; i < boardGemsBuffer.Length; i++)
                {
                    boardGemsBufferArray[i] = boardGemsBuffer[i];
                }

                var gemsCheckMatchJob = new GemsCheckMatchJob
                {
                    BoardEntity = boardEntity,
                    BoardDataLookup = m_BoardDataLookup,
                    GemStateLookup = m_GemStateLookup,
                    BoardGemsBuffer = boardGemsBufferArray,
                    ParallelWriter = ecb.AsParallelWriter(),
                    GemStatesParallelWriter = gemStates.AsParallelWriter(),
                };

                var query = SystemAPI.QueryBuilder().WithAll<GemState>().Build();

                var gemsCheckMatchJobHandle = gemsCheckMatchJob.ScheduleParallel(query, state.Dependency);

                var setBoardStateJob = new SetBoardGemStateWithConditionJob
                {
                    BoardEntity = boardEntity,
                    BoardState = boardState.ValueRO,
                    GemStates = gemStates,
                    ECB = ecb,
                    TrueState = EBoardGemState.Destroy,
                    FalseState = EBoardGemState.Idle,
                    GemStateToCheck = EGemState.Destroy
                };

                var setBoardStateJobHandle = setBoardStateJob.Schedule(gemsCheckMatchJobHandle);
                gemStates.Dispose(setBoardStateJobHandle);
                boardGemsBufferArray.Dispose(setBoardStateJobHandle);
                state.Dependency = setBoardStateJobHandle;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct GemsCheckMatchJob : IJobEntity
    {
        public Entity BoardEntity;

        [ReadOnly]
        public ComponentLookup<BoardData> BoardDataLookup;
        [ReadOnly]
        public ComponentLookup<GemState> GemStateLookup;
        [ReadOnly]
        public NativeArray<BoardGemsBuffer> BoardGemsBuffer;

        public EntityCommandBuffer.ParallelWriter ParallelWriter;
        public NativeList<GemState>.ParallelWriter GemStatesParallelWriter;

        public void Execute(Entity gemEntity, [EntityIndexInQuery] int sortKey, in GemState gemState)
        {
            if (!BoardDataLookup.HasComponent(BoardEntity)) return;


            var boardData = BoardDataLookup.GetRefRO(BoardEntity);

            if ((gemState.GemPosition / boardData.ValueRO.BoardSize.x) >= boardData.ValueRO.PlayArea)
                return;

            var neighbors = boardData.ValueRO.GetNeighbors(gemState);
            var isMatch = true;
            var currentGemState = gemState;

            var matchGroup = new int4(0, 0, 0, 0);

            while (isMatch)
            {
                isMatch = CheckNeighborsMatch(BoardGemsBuffer, currentGemState, neighbors.x, GemStateLookup);

                if(isMatch)
                {
                    Entity neighborsEntity = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(neighbors.x).GemEntity;
                    matchGroup.x++;
                    currentGemState = GemStateLookup.GetRefRO(neighborsEntity).ValueRO;
                    neighbors = boardData.ValueRO.GetNeighbors(currentGemState);
                }
            }

            neighbors = boardData.ValueRO.GetNeighbors(gemState);
            isMatch = true;
            currentGemState = gemState;

            while (isMatch)
            {
                isMatch = CheckNeighborsMatch(BoardGemsBuffer, currentGemState, neighbors.y, GemStateLookup);

                if (isMatch)
                {
                    Entity neighborsEntity = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(neighbors.y).GemEntity;
                    matchGroup.y++;
                    currentGemState = GemStateLookup.GetRefRO(neighborsEntity).ValueRO;
                    neighbors = boardData.ValueRO.GetNeighbors(currentGemState);
                }
            }

            neighbors = boardData.ValueRO.GetNeighbors(gemState);
            isMatch = true;
            currentGemState = gemState;

            while (isMatch)
            {
                isMatch = CheckNeighborsMatch(BoardGemsBuffer, currentGemState, neighbors.z, GemStateLookup);

                if (isMatch)
                {
                    Entity neighborsEntity = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(neighbors.z).GemEntity;
                    matchGroup.z++;
                    currentGemState = GemStateLookup.GetRefRO(neighborsEntity).ValueRO;
                    neighbors = boardData.ValueRO.GetNeighbors(currentGemState);
                }
            }

            neighbors = boardData.ValueRO.GetNeighbors(gemState);
            isMatch = true;
            currentGemState = gemState;

            while (isMatch)
            {
                isMatch = CheckNeighborsMatch(BoardGemsBuffer, currentGemState, neighbors.w, GemStateLookup);

                if (isMatch)
                {
                    Entity neighborsEntity = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(neighbors.w).GemEntity;
                    matchGroup.w++;
                    currentGemState = GemStateLookup.GetRefRO(neighborsEntity).ValueRO;
                    neighbors = boardData.ValueRO.GetNeighbors(currentGemState);
                }
            }
            
            if(matchGroup.x + matchGroup.z >= 2)
            {
                for (int i = 0; i < matchGroup.x; i++)
                {
                    var e = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(gemState.GemPosition - (i + 1)).GemEntity;
                    var eGemState = GemStateLookup.GetRefRO(e).ValueRO;
                    eGemState.CurrentState = EGemState.Destroy;
                    ParallelWriter.SetComponent(sortKey, e, eGemState);
                    GemStatesParallelWriter.AddNoResize(eGemState);
                }

                for (int i = 0; i < matchGroup.z; i++)
                {
                    var e = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(gemState.GemPosition + (i + 1)).GemEntity;
                    var eGemState = GemStateLookup.GetRefRO(e).ValueRO;
                    eGemState.CurrentState = EGemState.Destroy;
                    ParallelWriter.SetComponent(sortKey, e, eGemState);
                    GemStatesParallelWriter.AddNoResize(eGemState);
                }

                var mGemState = gemState;
                mGemState.CurrentState = EGemState.Destroy;
                ParallelWriter.SetComponent(sortKey, gemEntity, mGemState);
                GemStatesParallelWriter.AddNoResize(mGemState);
            }

            if (matchGroup.y + matchGroup.w >= 2)
            {
                for (int i = 0; i < matchGroup.y; i++)
                {
                    var e = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(gemState.GemPosition + ((i + 1) * boardData.ValueRO.BoardSize.x)).GemEntity;
                    var eGemState = GemStateLookup.GetRefRO(e).ValueRO;
                    eGemState.CurrentState = EGemState.Destroy;
                    ParallelWriter.SetComponent(sortKey, e, eGemState);
                    GemStatesParallelWriter.AddNoResize(eGemState);
                }

                for (int i = 0; i < matchGroup.w; i++)
                {
                    var e = BoardGemsBuffer.GetBoardGemsBufferFromGemPosistion(gemState.GemPosition - ((i + 1) * boardData.ValueRO.BoardSize.x)).GemEntity;
                    var eGemState = GemStateLookup.GetRefRO(e).ValueRO;
                    eGemState.CurrentState = EGemState.Destroy;
                    ParallelWriter.SetComponent(sortKey, e, eGemState);
                    GemStatesParallelWriter.AddNoResize(eGemState);
                }

                var mGemState = gemState;
                mGemState.CurrentState = EGemState.Destroy;
                ParallelWriter.SetComponent(sortKey, gemEntity, mGemState);
                GemStatesParallelWriter.AddNoResize(mGemState);
            }
        }

        private bool CheckNeighborsMatch(NativeArray<BoardGemsBuffer> boardBuffer, GemState gemState, int neighborsPosition, ComponentLookup<GemState> gemStateLookup)
        {
            var boardBufferGem = boardBuffer.GetBoardGemsBufferFromGemPosistion(neighborsPosition);

            if (neighborsPosition < 0 || boardBufferGem.GemEntity == Entity.Null)
                return false;

            Entity neighborsEntity = boardBufferGem.GemEntity;

            return gemStateLookup.GetRefRO(neighborsEntity).ValueRO.ColorTag == gemState.ColorTag;
        }
    }
}