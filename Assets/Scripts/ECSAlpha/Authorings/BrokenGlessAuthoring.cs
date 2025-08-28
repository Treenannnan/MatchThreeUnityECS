using ECSAlpha.DOTS.Tags;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.Authorings
{
    public class BrokenGlessAuthoring : MonoBehaviour
    {
        
    }

    public class BrokenGlessBaker : Baker<BrokenGlessAuthoring>
    {
        public override void Bake(BrokenGlessAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<BrokenGlessTag>(entity);
        }
    }
}