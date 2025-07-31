using Thanos.Tests.Support.Requests;

namespace Thanos.Tests.Support;

public class TestModel(byte[] json, MoveRequest state)
{
    public byte[] Json { get; } = json;
    public MoveRequest State { get; } = state;
}