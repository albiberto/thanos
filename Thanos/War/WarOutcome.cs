namespace Thanos.War;

public enum WarOutcome : byte
{
    /// <summary>
    /// La battaglia è ancora in corso, più di un serpente è vivo.
    /// </summary>
    Ongoing,
    /// <summary>
    /// La battaglia è terminata con un vincitore.
    /// </summary>
    Victory,
    /// <summary>
    /// La battaglia è terminata senza vincitori (tutti i serpenti sono morti).
    /// </summary>
    Draw
}