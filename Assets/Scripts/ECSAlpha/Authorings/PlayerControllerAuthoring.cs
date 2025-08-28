using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Tags;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.Authorings
{
    public class PlayerControllerAuthoring : MonoBehaviour
    {

    }

    class PlayerControllerBaker : Baker<PlayerControllerAuthoring>
    {
        public override void Bake(PlayerControllerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent<PlayerControllerTag>(entity);

            AddBuffer<MouseInputDataBuffer>(entity);

            AddComponent<PlayerControllerData>(entity);
        }
    }
}