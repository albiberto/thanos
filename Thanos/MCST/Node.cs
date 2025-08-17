using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.MCST;

/// <summary>
/// Rappresenta un nodo nell'albero di ricerca Monte Carlo.
/// Contiene le statistiche di vittoria/visite e i puntatori ai nodi figli e genitore.
/// </summary>
public unsafe struct Node
{
    // Puntatori per la navigazione dell'albero
    public Node* Parent, Child1, Child2, Child3;

    public uint ChildrenCount;
    
    // Statistiche per la formula UCT (Upper Confidence bound for Trees)
    public long Visits;
    public double Wins;

    public MoveDirection MoveThatLedToThisNode; // La mossa che ha generato questo stato
    public bool IsTerminal; // True se lo stato del gioco è finale (vittoria/sconfitta)
    
    public readonly bool IsLeaf => ChildrenCount == 0;
    
    public Node* GetBestChild()
    {
        Node* bestChild = null;
        var bestScore = double.NegativeInfinity;
    
        // Costante di esplorazione. Un valore più alto favorisce l'esplorazione.
        const double explorationConstant = 1.414; // Math.Sqrt(2)

        // Itera sui figli del nodo corrente (in questo caso, supponiamo 3 figli)
        // In un'implementazione reale, useresti un array o una lista di puntatori ai figli.
        Node*[] children = [Child1, Child2, Child3];
    
        foreach (var child in children)
        {
            if (child == null) continue; // Salta se il figlio non esiste

            // --- Caso Speciale: Esplorazione garantita ---
            // Se un figlio non è mai stato visitato, ha la priorità assoluta.
            // Il suo punteggio di esplorazione è infinito. Lo scegliamo subito.
            if (child->Visits == 0)
            {
                return child;
            }

            // --- Calcolo della Formula UCT ---

            // 1. Parte di Sfruttamento (Win Rate)
            var exploitationScore = child->Wins / child->Visits;

            // 2. Parte di Esplorazione
            var explorationScore = explorationConstant * System.Math.Sqrt(System.Math.Log(Visits) / child->Visits);

            // 3. Punteggio Totale
            var uctScore = exploitationScore + explorationScore;

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChild = child;
            }
        }

        return bestChild;
    }
    
    public unsafe void AddChild(Node* child, MoveDirection move)
    {
        // Trova il prossimo slot libero in base al numero di figli attuali.
        switch (ChildrenCount)
        {
            case 0:
                Child1 = child;
                break;
            case 1:
                Child2 = child;
                break;
            case 2:
                Child3 = child;
                break;
            default:
                // Se arriviamo qui, stiamo cercando di aggiungere più figli di quanti ne possiamo contenere.
                // Questo indica un errore nella logica dell'algoritmo.
                // In un'applicazione reale, qui si potrebbe lanciare un'eccezione.
                return;
        }

        // Imposta il puntatore del figlio verso questo nodo (il genitore).
        // 'Unsafe.AsPointer(ref this)' ottiene l'indirizzo di memoria della struct corrente.
        child->Parent = (Node*)Unsafe.AsPointer(ref this);

        // Memorizza la mossa che ha portato alla creazione di questo figlio.
        child->MoveThatLedToThisNode = move;
    
        // Incrementa il contatore dei figli.
        ChildrenCount++;
    }
}