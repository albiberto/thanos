// using Thanos.Domain;
// using Thanos.Enums;
// using Thanos.Model;
//
// namespace Thanos.CollisionMatrix;
//
// public class GetBestMoveLightSpeedB
// {
//     private static readonly Direction[] _validMoves = new Direction[4];
//     
//     public static int GetBestMoveLightSpeed(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
//     {
//         var moves = GetValidMovesLightSpeedB.GetValidMovesLightSpeed(width, height, myId, myBody, myBodyLength, myHeadX, myHeadY, hazards, hazardCount, snakes, snakeCount, eat);
//         var count = moves;
//         
//         // Branchless early exit: se count=0 ritorna -1, se count=1 ritorna il valore
//         // Usa bit manipulation per contare i bit settati
//         count -= ((count >> 1) & 0x55555555);
//         count = (count & 0x33333333) + ((count >> 2) & 0x33333333);
//         count = ((count + (count >> 4)) & 0x0F0F0F0F) * 0x01010101 >> 24;
//         
//         // Branchless return:
//         // se count=0 → -1,
//         // se count=1 → result,
//         // altrimenti → continua
//         return ((count - 1) >> 31) * (-1 - moves) + moves;
//     }
// }