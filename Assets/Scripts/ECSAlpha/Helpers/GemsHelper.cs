using ECSAlpha.DOTS.Tags;
using Unity.Mathematics;

namespace ECSAlpha.Hepers
{
    public static class GemsHelper
    {
        public const int MAX_GEM_COLOR_TAGS = 10;

        public static EGemColorTags GetColorByNumber(int number)
        {
            switch (number)
            {
                case 0: return EGemColorTags.Red;
                case 1: return EGemColorTags.Blue;
                case 2: return EGemColorTags.Green;
                case 3: return EGemColorTags.Yellow;
                case 4: return EGemColorTags.Purple;
                case 5: return EGemColorTags.Orange;
                case 6: return EGemColorTags.Turquoise;
                case 7: return EGemColorTags.Fuchsia;
                case 8: return EGemColorTags.Brown;
                case 9: return EGemColorTags.White;
            }

            return EGemColorTags.None;
        }

        public static float4 ColorValue(this EGemColorTags colorTag)
        {
            switch(colorTag)
            {
                case EGemColorTags.Red: return new float4(1, 0, 0, 1);
                case EGemColorTags.Blue: return new float4(0, 0, 1, 1);
                case EGemColorTags.Green: return new float4(0, 1, 0, 1);
                case EGemColorTags.Yellow: return new float4(1, 1, 0, 1);
                case EGemColorTags.Purple: return new float4(1, 0, 1, 1);
                case EGemColorTags.Orange: return new float4(1, 0.3f, 0, 1);
                case EGemColorTags.Turquoise: return new float4(0.25098f, 0.87843f, 0.81568f, 1);
                case EGemColorTags.Fuchsia: return new float4(0.7f, 0.8f, 0.2f, 1);
                case EGemColorTags.Brown: return new float4(0.54509f, 0.27058f, 0.07451f, 1);
                case EGemColorTags.White: return new float4(1, 1, 1, 1);
            }

            return new float4(0, 0, 0, 0);
        }
    }
}