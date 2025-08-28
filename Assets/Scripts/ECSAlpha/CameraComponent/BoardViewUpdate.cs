using ECSAlpha.DOTS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.CameraComponent
{
    [RequireComponent(typeof(Camera))]
    public class BoardViewUpdate : MonoBehaviour
    {
        private Entity m_BoardEntity;
        private EntityManager m_EntityManager;
        private Camera m_Camera;

        void Start()
        {
            m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            m_Camera = GetComponent<Camera>();
        }

        void Update()
        {
            if (m_BoardEntity == null) return;

            if (!m_EntityManager.Exists(m_BoardEntity)) return;
            if (!m_EntityManager.HasComponent<BoardView>(m_BoardEntity)) return;

            var boardViewComp = m_EntityManager.GetComponentData<BoardView>(m_BoardEntity);

            if (boardViewComp.CameraSize != m_Camera.orthographicSize)
                m_Camera.orthographicSize = boardViewComp.CameraSize;
        }

        public void SetBoardEntity(Entity boardEntity)
        {
            m_BoardEntity = boardEntity;
        }
    }
}
