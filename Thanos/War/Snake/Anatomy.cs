namespace Thanos.War.Snake;

public struct Anatomy(int capacity, int length)
{
    public bool WillGrow { get; private set; }
    
    public int TailIndex { get; private set; }
    public int Length { get; private set; } = length;
    public int Capacity { get; } = capacity;
    public int CapacityMask { get; } = capacity - 1;

    public bool IsFull => Length == Capacity;

    public readonly int HeadIndex => (TailIndex + Length - 1) & CapacityMask;
    public readonly int NextHeadIndex => (TailIndex + Length) & CapacityMask;

    public void PopTail() => TailIndex = (TailIndex + 1) & CapacityMask;

    public void IncrementLength()
    {
        if (Length < Capacity) Length++;
    }
    
    public void UpdateAfterMove(bool hasEaten)
    {
        // PRIMA, gestiamo la crescita pendente dal turno precedente.
        if (WillGrow)
        {
            Length++;       // La lunghezza aumenta.
            WillGrow = false; // Abbiamo "usato" la crescita.
            // La coda NON si muove (PopTail non viene chiamato).
        }
        else
        {
            // Se non dovevamo crescere, la coda si muove normalmente.
            TailIndex = (TailIndex + 1) & CapacityMask;
        }

        // POI, se abbiamo mangiato ORA, impostiamo il flag per il PROSSIMO turno.
        if (hasEaten)
        {
            WillGrow = true;
        }
    }
}