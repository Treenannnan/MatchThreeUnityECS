using ECSAlpha.DOTS.Components;
using ECSAlpha.DOTS.Tags;
using Unity.Entities;
using UnityEngine;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    partial class PlayerInputSystem : SystemBase
    {
        private PlayerInputActions m_PlayerInputActions;

        protected override void OnCreate()
        {
            m_PlayerInputActions = new PlayerInputActions();
            m_PlayerInputActions.Enable();

            m_PlayerInputActions.UI.Click.performed += OnClick;
        }

        private void OnClick(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (m_PlayerInputActions == null || Camera.main == null) return;

            var point = m_PlayerInputActions.UI.Point.ReadValue<Vector2>();
            var click = obj.ReadValue<float>() > 0;

            foreach (var (playerTag, entity) in SystemAPI.Query<RefRO<PlayerControllerTag>>().WithEntityAccess())
            {
                var clickBuffer = SystemAPI.GetBuffer<MouseInputDataBuffer>(entity);
                var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, Camera.main.transform.position.z * -1));

                clickBuffer.Add(new MouseInputDataBuffer
                {
                    MouseWorldPosition = new Unity.Mathematics.float2(worldPos.x, worldPos.y),
                    IsPress = click
                });
            }
        }

        protected override void OnUpdate() { }
        /*
        protected override void OnUpdate()
        {
            if (m_PlayerInputActions == null || Camera.main == null) return;

            var point = m_PlayerInputActions.UI.Point.ReadValue<Vector2>();
            var click = m_PlayerInputActions.UI.Click.ReadValue<float>() > 0;

            foreach (var (mouneInput, entity) in SystemAPI.Query<RefRW<MouseInputData>>().WithAll<PlayerControllerTag>().WithEntityAccess())
            {
                mouneInput.ValueRW.MouseWorldPosition = Camera.main.ScreenToWorldPoint(point);
                mouneInput.ValueRW.IsPress = click;
            }
        }
        */
        protected override void OnDestroy()
        {
            if (m_PlayerInputActions != null)
            {
                m_PlayerInputActions.UI.Click.performed -= OnClick;
                m_PlayerInputActions.Dispose();
            }
        }

    }
}
