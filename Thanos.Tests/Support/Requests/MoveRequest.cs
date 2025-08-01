using System.Text.Json.Serialization;
using Thanos.Tests.Support.Model;

namespace Thanos.Tests.Support.Requests;

[method: JsonConstructor]
public class MoveRequest(uint turn, Board board, BattleSnake you)
{
    public uint Turn { get; } = turn;
    public Board Board { get; } = board;
    public BattleSnake You { get; } = you;
}
