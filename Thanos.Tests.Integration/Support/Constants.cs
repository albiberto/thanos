namespace Thanos.Tests.Integration.Support;

public static class Constants
{
    public static string MediumJson => File.ReadAllText("Requests/MediumRequest.json");
    public static string SmallJson => File.ReadAllText("Requests/SmallRequest.json");

    // --- IDENTITIES ---
    public const string Me = "snake-hero";
    public const string Enemy1 = "snake-enemy";
    public const string Enemy2 = "snake-enemy-2";
    public const string Enemy3 = "snake-enemy-3";
}