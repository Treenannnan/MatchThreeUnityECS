using ECSAlpha.DOTS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace ECSAlpha.DOTS.Jobs
{
    [BurstCompile]
    public partial struct SetBoardStateJob : IJob
    {
        public Entity BoardEntity;
        public BoardState BoardState;

        public EBoardState NewState;

        public EntityCommandBuffer ECB;

        public void Execute()
        {
            BoardState.CurrentBoardState = NewState;

            ECB.SetComponent(BoardEntity, BoardState);
        }
    }

    [BurstCompile]
    public partial struct SetBoardGemStateJob : IJob
    {
        public Entity BoardEntity;
        public BoardState BoardState;
        public EBoardGemState NewState;
        public EntityCommandBuffer ECB;

        public void Execute()
        {
            BoardState.CurrentGemState = NewState;

            ECB.SetComponent(BoardEntity, BoardState);
        }
    }

    [BurstCompile]
    public partial struct SetBoardGemStateWithBoardGemsBufferJob : IJob
    {
        public Entity BoardEntity;
        public BoardState BoardState;
        public NativeList<GemState> GemStates;

        public EBoardGemState NewState;

        public EntityCommandBuffer ECB;

        public void Execute()
        {
            foreach (var i in GemStates)
            {
                if (i.ColorTag == Tags.EGemColorTags.None)
                    continue;

                if (i.CurrentState != EGemState.Idel)
                {
                    return;
                }
            }

            BoardState.CurrentGemState = NewState;
            ECB.SetComponent(BoardEntity, BoardState);
        }
    }

    [BurstCompile]
    public partial struct SetBoardGemStateWithConditionJob : IJob
    {
        public Entity BoardEntity;
        public BoardState BoardState;
        public NativeList<GemState> GemStates;

        public EBoardGemState TrueState;
        public EBoardGemState FalseState;

        public EGemState GemStateToCheck;

        public EntityCommandBuffer ECB;

        public void Execute()
        {
            foreach (var i in GemStates)
            {
                if (i.ColorTag != Tags.EGemColorTags.None && i.CurrentState == GemStateToCheck)
                {
                    BoardState.CurrentGemState = TrueState;
                    ECB.SetComponent(BoardEntity, BoardState);
                    return;
                }
            }

            BoardState.CurrentGemState = FalseState;
            ECB.SetComponent(BoardEntity, BoardState);
        }
    }

    [BurstCompile]
    public partial struct BoardGemsBufferUpdateJob : IJobEntity
    {
        [ReadOnly]
        public NativeList<BoardGemsBuffer> BoardGemsBufferUpdate;
        public void Execute(in Entity entity, ref DynamicBuffer<BoardGemsBuffer> boardGemsBuffer)
        {
            foreach (var bg in BoardGemsBufferUpdate)
            {
                boardGemsBuffer[bg.Index] = bg;
            }
        }
    }
}