namespace Thanos.Tests.SourceGen;

public static class Support
{
    public static string MediumJson => File.ReadAllText("Requests/MediumRequest.json");
    public static string SmallJson => File.ReadAllText("Requests/SmallRequest.json");

    public const string Me = "snake-hero";       
    public const string Enemy1 = "snake-enemy";     
    public const string Enemy2 = "snake-enemy-2";   
    public const string Enemy3 = "snake-enemy-3";  
    
    public static object[][] Dimensions =>
    [
        [Constants.Medium.Width, Constants.Medium.Height, Constants.Medium.Area]
    ];
}