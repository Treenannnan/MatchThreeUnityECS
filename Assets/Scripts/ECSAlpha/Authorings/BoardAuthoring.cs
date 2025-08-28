using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Tags;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.Authorings
{
    public class BoardAuthoring : MonoBehaviour
    {
        [field: SerializeField] public Vector2 BoardSize {  get; private set; }
        [field: SerializeField] public GemAuthoring GemPrefab { get; private set; }
        [field: SerializeField] public  BrokenGlessAuthoring BrokenGlessPrefab { get; private set; }
    }

    class BoardBaker : Baker<BoardAuthoring>
    {
        public override void Bake(BoardAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new BoardData
            {
                BoardSize = new Unity.Mathematics.int2((int)authoring.BoardSize.x, (int)authoring.BoardSize.y),
                GemPrefab = GetEntity(authoring.GemPrefab, TransformUsageFlags.Dynamic),
                BrokenGlessPrefab = GetEntity(authoring.BrokenGlessPrefab, TransformUsageFlags.Dynamic),
                BoardGroundPosition = authoring.transform.position.y,
                BoardDeepPosition = authoring.transform.position.z,
                PlayArea = 16
            });

            AddComponent(entity, new BoardState
            {
                CurrentBoardState = EBoardState.None,
                CurrentGemState = EBoardGemState.None,
            });

            AddComponent(entity, new BoardView
            {
                CameraSize = authoring.BoardSize.x + 1,
                CurrentBoardViewState = EBoardViewState.None
            });

            AddBuffer<BoardGemsBuffer>(entity);

            AddComponent<BoardTag>(entity);
        }
    }
}