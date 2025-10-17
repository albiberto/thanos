// Thanos/War/WarSnakeHeader.cs

using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeHeader
{
    private const byte FullHealth = 100;

    public ushort Head;
    public byte HP;
    private byte _isPendingGrowth; // Manteniamo questo per gestire la crescita nel turno successivo

    // Indici per il buffer circolare
    public int HeadIndex;
    public int TailIndex;

    public readonly bool IsDead => HP <= 0;
    public readonly bool IsGrowthPending => _isPendingGrowth == 1;
    
    public void ScheduleGrowth() => _isPendingGrowth = 1;

    // Questo metodo verrà chiamato all'inizio della mossa del serpente
    public void ProcessPendingGrowth(ref ushort length)
    {
        if (_isPendingGrowth == 0) return;
        length++;
        _isPendingGrowth = 0;
    }

    public void Damage(byte amount)
    {
        if (HP > amount)
            HP -= amount;
        else
            HP = 0;
    }

    public void Kill() => HP = 0;
    public void FullCure() => HP = FullHealth;

    public void PlacementNew(ushort head, byte points, ushort length)
    {
        Head = head;
        HP = points;
        // La lunghezza non viene più scritta qui direttamente, ma gestita dal buffer
        _isPendingGrowth = 0;
        HeadIndex = 0;
        TailIndex = 0;
    }
}