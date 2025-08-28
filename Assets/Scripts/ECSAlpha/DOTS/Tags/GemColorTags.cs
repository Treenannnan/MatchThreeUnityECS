using Unity.Entities;

namespace ECSAlpha.DOTS.Tags
{
    public struct GemColorRedTag : IComponentData { } //  #FF0000
    public struct GemColorGreenTag : IComponentData { } // #00FF00
    public struct GemColorBlueTag : IComponentData { } // #0000FF
    public struct GemColorYellowTag : IComponentData { } // #FFFF00
    public struct GemColorPurpleTag : IComponentData { } // #800080
    public struct GemColorOrangeTag : IComponentData { } // #FFA500
    public struct GemColorTurquoiseTag : IComponentData { } // #40E0D0
    public struct GemColorFuchsiaTag : IComponentData { } // #FF00FF
    public struct GemColorBrownTag : IComponentData { } // #8B4513
    public struct GemColorWhiteTag : IComponentData { } // #FFFFFF

    public enum EGemColorTags
    {
        None = 0,
        Red,
        Green,
        Blue,
        Yellow,
        Purple,
        Orange,
        Turquoise,
        Fuchsia,
        Brown,
        White,
    }
}
