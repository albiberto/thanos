using Thanos.Domain;
using Thanos.Enums;
using Thanos.Model;

namespace Thanos.CollisionMatrix;

public static class GetBestMoveUltraFastB
{
    private static readonly Direction[] _validMoves = new Direction[4];
    
    public static Direction GetBestMoveUltraFast(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        var movesCount = GetValidMovesUltraFastB.GetValidMovesUltraFast(width, height, myId, myBody, myBodyLength, myHeadX, myHeadY, hazards, hazardCount, snakes, snakeCount, eat);
        
        // Gestione casi limite
        if (movesCount < 1) return Direction.Up;
        if (movesCount == 1) return _validMoves[0];
        
        return _validMoves[1];
    }
}