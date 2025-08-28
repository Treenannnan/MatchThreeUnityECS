using ECSAlpha.DOTS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.Authorings
{
    public class CannonAuthoring : MonoBehaviour
    {
        public int MaxBullet;
    }

    class CannonBaker : Baker<CannonAuthoring>
    {
        public override void Bake(CannonAuthoring authoring)
        {
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new CannonData
                {
                    MaxBullet = authoring.MaxBullet
                });

                AddComponent(entity, new CannonState
                {
                    CurrentBulletColor = DOTS.Tags.EGemColorTags.None,
                    NextBulletColor = DOTS.Tags.EGemColorTags.None,
                    CurrentBulletAmount = authoring.MaxBullet
                });
            }
        }
    }
}