namespace Thanos.Enums;

public static class Constants
{
    public const int SizeOfCacheLine = 64;

    public const int MaxNodes = 500_000;
    
    public const int MaxWidth = 19;
    public const int MaxHeight = 19;
    public const int MaxArea = MaxWidth * MaxHeight;
    
    public const int MaxSnakes = 8; 
    public const uint MaxSnakeBodyCapacity = 256;
    
    public const byte Empty = 0; 
    public const byte Me = 1;    
    public const byte Enemy1 = 2;
    public const byte Enemy2 = 3;
    public const byte Enemy3 = 4;
    public const byte Enemy4 = 5;
    public const byte Enemy5 = 6;
    public const byte Enemy6 = 7;
    public const byte Enemy7 = 8;
    public const byte Food = 127;
    public const byte Hazard = 255;
    
}