namespace Thanos;

public static class GameManager
{
    private static BattleField BattleField;
    
    static GameManager()
    {
        BattleField = new BattleField();
        BattleField.Initialize();
    }
}