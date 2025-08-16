namespace Thanos.MCST;

/// <summary>
/// Rappresenta un nodo nell'albero di ricerca Monte Carlo.
/// Contiene le statistiche di vittoria/visite e i puntatori ai nodi figli e genitore.
/// </summary>
public unsafe struct Node
{
    // Puntatori per la navigazione dell'albero
    public Node* Parent, Child1, Child2, Child3;

    public ushort ChildrenCount;
    
    // Statistiche per la formula UCT (Upper Confidence bound for Trees)
    public long Visits;
    public double Wins;

    public MoveDirection? MoveThatLedToThisNode; // La mossa che ha generato questo stato
    public bool IsTerminal; // True se lo stato del gioco è finale (vittoria/sconfitta)
}