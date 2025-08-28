using Unity.Entities;

namespace ECSAlpha.DOTS.Components
{
    public enum EBoardViewState
    {
        None = 0,
        Moving
    }


    public struct BoardView : IComponentData
    {
        public float CameraSize;
        public EBoardViewState CurrentBoardViewState;
    }
}
