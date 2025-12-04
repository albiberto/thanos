namespace Thanos.Tests;

public static class Support
{
    public static string SampleJson => File.ReadAllText("Requests/SampleRequest.json");
    
    public const string Me = "snake-hero";       
    
    public const string Enemy1 = "snake-enemy";     
    public const string Enemy2 = "snake-enemy-2";   
    public const string Enemy3 = "snake-enemy-3";  
    
    public static object[][] Dimensions =>
    [
        [Constants.Small.Width, Constants.Small.Height, Constants.Small.Area],
        [Constants.Medium.Width, Constants.Medium.Height, Constants.Medium.Area],
        [Constants.Large.Width, Constants.Large.Height, Constants.Large.Area]
    ];
}