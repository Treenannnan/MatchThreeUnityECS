using ECSAlpha.DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSAlpha.Hepers
{
    public static class BoardHelper
    {
        /// <summary>
        /// return a neighbors position on board.
        /// </summary>
        /// <returns> int4{x=left y=top z=right w=bottom} and -1 is wall </returns>
        public static int4 GetNeighbors(this BoardData boardData, GemState gemState)
        {
            var result = new int4();

            int posY = gemState.GemPosition / boardData.BoardSize.x;
            int posX = gemState.GemPosition - (posY * boardData.BoardSize.x);

            result.x = posX == 0 ? -1 : (posY * boardData.BoardSize.x) + (posX - 1);
            result.y = posY == boardData.BoardSize.y - 1 ? -1 : ((posY + 1) * boardData.BoardSize.x) + posX;
            result.z = posX == boardData.BoardSize.x - 1 ? -1 : (posY * boardData.BoardSize.x) + (posX + 1);
            result.w = posY == 0 ? -1 : ((posY - 1) * boardData.BoardSize.x) + posX;

            return result;
        }

        /// <summary>
        /// return a world position of the gem position on the board.
        /// </summary>
        /// <param name="position"> gem position on board (1d)</param>
        public static float3 GetLocalPositionFromGemPosistion(this BoardData boardData, int position)
        {
            var result = new float2();

            result.y = position / boardData.BoardSize.x;
            result.x = position - (result.y * boardData.BoardSize.x);

            return new float3(result.x - (boardData.BoardSize.x - 1) / 2.0f, result.y + boardData.BoardGroundPosition, boardData.BoardDeepPosition);
        }

        public static BoardGemsBuffer GetBoardGemsBufferFromGemPosistion(this NativeArray<BoardGemsBuffer> boardGemsBuffers, int position)
        {
            foreach (var buffer in boardGemsBuffers)
            {
                if(buffer.Index == position)
                    return buffer;
            }

            return new BoardGemsBuffer();
        }

        public static BoardGemsBuffer GetBoardGemsBufferFromGemPosistion(this DynamicBuffer<BoardGemsBuffer> boardGemsBuffers, int position)
        {
            foreach (var buffer in boardGemsBuffers)
            {
                if (buffer.Index == position)
                    return buffer;
            }

            return new BoardGemsBuffer();
        }
    }
}