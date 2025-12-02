namespace Thanos.Tests;

public static class Support
{
    public static object[][] Dimensions =>
    [
        [Constants.Small.Width, Constants.Small.Height, Constants.Small.Area],
        [Constants.Medium.Width, Constants.Medium.Height, Constants.Medium.Area],
        [Constants.Large.Width, Constants.Large.Height, Constants.Large.Area]
    ];
}