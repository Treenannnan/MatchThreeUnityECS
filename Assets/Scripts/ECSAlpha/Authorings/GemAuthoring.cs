using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Tags;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.Authorings
{
    public class GemAuthoring : MonoBehaviour
    {

    }

    class GemBaker : Baker<GemAuthoring>
    {
        public override void Bake(GemAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<GemTag>(entity);

            AddComponent(entity, new GemState
            {
                GemPosition = -1,
                CurrentState = EGemState.None,
                IsSelected = false
            });

            AddComponent<GemTransformState>(entity);
            AddComponent<GemData>(entity);


        }
    }
}
