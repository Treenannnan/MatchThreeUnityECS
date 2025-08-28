using Unity.Entities;

namespace ECSAlpha.DOTS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class InputSystemGroup : ComponentSystemGroup { }

    [UpdateAfter(typeof(InputSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class InputSimulateSystemGroup : ComponentSystemGroup { }

    [UpdateAfter(typeof(InputSimulateSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class InputClearSystemGroup : ComponentSystemGroup { }

    [UpdateAfter(typeof(InputClearSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PhysicsSimulateSystemGroup : ComponentSystemGroup { }

    [UpdateAfter(typeof(PhysicsSimulateSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PhysicsCollisionsSystemGroup : ComponentSystemGroup { }

    [UpdateAfter(typeof(PhysicsCollisionsSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameplayCalculateSystemGroup : ComponentSystemGroup { }

    [UpdateAfter(typeof(GameplayCalculateSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RanderSystemGroup : ComponentSystemGroup { }


    [UpdateBefore(typeof(GameplayCalculateSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class UICalculateSystemGroup : ComponentSystemGroup { }
}
