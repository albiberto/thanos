// In HeuristicWeights.cs
namespace Thanos;

public static class HeuristicWeights
{
    // --- PESI RIVISTI E BILANCIATI ---

    // La posizione è importante. Una penalità forte ma non esagerata.
    public const double BorderPenaltyValue = -50.0; 
    
    // Vogliamo un incentivo più forte a controllare il centro.
    public const double CenterBonusValue = 15.0; 
    
    // Il peso dello spazio ora è molto più piccolo.
    // Il valore di `EstimateSafeSpaceBitset` (es. 20) ORA è il punteggio.
    // Lo scaliamo solo leggermente per dargli più o meno importanza.
    public const double SpaceWeight = 1.5; 
    
    // Il cibo è un bonus, non una priorità assoluta.
    public const double FoodWeight = 20.0; 
    
    public const int SafeSpaceNodeBudget = 512;

    // MobilityBonusValue non serve più perché abbiamo rimosso la mobilità statica dalla LUT.
    // public const double MobilityBonusValue = 1.0; 
}