using ECSAlpha.DOTS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.ECSWrappers
{
    public class ECSWrapper<T> : MonoBehaviour where T : unmanaged, IComponentData
    {
        public Entity ECSEntity {  get; private set; }

        public EntityManager EntityManager { get; private set; }

        void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Update()
        {
            if (ECSEntity == null || ECSEntity == Entity.Null) return;
            if (EntityManager == null) return;
            if (!EntityManager.Exists(ECSEntity)) return;
            if (!EntityManager.HasComponent<T>(ECSEntity)) return;

            var componentData = EntityManager.GetComponentData<T>(ECSEntity);

            OnUpdateAfterCheck(Time.deltaTime, componentData);
        }

        virtual protected void OnUpdateAfterCheck(float deltaTime, T compData) { }

        public void SetEntity(Entity boardEntity)
        {
            ECSEntity = boardEntity;
        }
    }
}
