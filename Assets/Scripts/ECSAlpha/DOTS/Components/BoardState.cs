using Unity.Entities;

namespace ECSAlpha.DOTS.Components
{
    public enum EBoardState
    {
        None = 0,
        Initialized,
        CreateGems,
        SetupGems,
        GamePreStart,
        GameStart,
        GameRunning,
        GamePreEnd,
        GameEnd
    }

    public enum EBoardGemState
    {
        None = 0,
        Idle,
        Select,
        Check,
        Destroy,
        Move,
        End
    }

    public struct BoardState : IComponentData
    {
        public EBoardState CurrentBoardState;
        public EBoardGemState CurrentGemState;
    }
}