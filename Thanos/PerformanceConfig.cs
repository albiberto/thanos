namespace Thanos;

public static class PerformanceConfig
{
    // 🚀 CONFIGURAZIONE RAM - Ridotta per stabilità
    private const uint AVAILABLE_RAM_GB = 8; // Ridotto a 8GB per sicurezza
    private const double RAM_USAGE_PERCENTAGE = 0.50; // Ridotto al 50% per stabilità

    // Calcoli automatici basati sulla RAM configurata
    public const ulong MaxMemoryBytes = (ulong)(AVAILABLE_RAM_GB * 1024L * 1024L * 1024L * RAM_USAGE_PERCENTAGE);
    public const uint MaxMemoryMB = (uint)(MaxMemoryBytes / 1024 / 1024);

    // Configurazione basata sul tempo
    private const float SimulationTimeRatio = 0.8f; // Percentuale di tempo dedicata alla simulazione
    private const float InitialExplorationTimeRatio = .2f; // Limite massimo per sicurezza
    private const float ExplorationTimeRatio = 1 - InitialExplorationTimeRatio; // Limite massimo per sicurezza

    public const int TotalSimulationTimeMs = 500;

    public const uint SimulationTimeMs = (uint)(TotalSimulationTimeMs * SimulationTimeRatio); // Tempo massimo per ogni mossa
    public const int InitialExplorationTimeMs = (int)(SimulationTimeMs * InitialExplorationTimeRatio); // Tempo massimo per la prima esplorazione
    public const uint ExplorationTimeMs = (uint)(SimulationTimeMs * ExplorationTimeRatio); // Tempo massimo per le esplorazioni successive
    public const uint MinSimulationsPerMove = 100; // Minimo per move non valida
    public const uint MaxSimulationsPerMove = 1000000; // Limite massimo per sicurezza
}