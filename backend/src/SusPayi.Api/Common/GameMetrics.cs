using System.Diagnostics.Metrics;

namespace SusPayi.Api.Common;

/// <summary>
/// Denge ayarı ve hile tespiti için oyun metrikleri. OpenTelemetry eklendiğinde
/// "SusPayi.Game" ölçer adıyla dışa aktarılır.
/// </summary>
internal sealed class GameMetrics
{
    public const string MeterName = "SusPayi.Game";

    private readonly Counter<long> _crimeAttempts;
    private readonly Counter<long> _bustAttempts;
    private readonly Counter<long> _promotions;

    public GameMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        var meter = meterFactory.Create(MeterName);
        _crimeAttempts = meter.CreateCounter<long>("suspayi.crime.attempts", description: "Suç denemeleri, sonuca göre");
        _bustAttempts = meter.CreateCounter<long>("suspayi.bust.attempts", description: "Hapisten kurtarma denemeleri, sonuca göre");
        _promotions = meter.CreateCounter<long>("suspayi.rank.promotions", description: "Rütbe atlamaları");
    }

    public void CrimeAttempted(string crimeId, string result) =>
        _crimeAttempts.Add(1, new KeyValuePair<string, object?>("crime", crimeId), new KeyValuePair<string, object?>("result", result));

    public void BustAttempted(bool freed) =>
        _bustAttempts.Add(1, new KeyValuePair<string, object?>("result", freed ? "freed" : "failed"));

    public void Promoted() => _promotions.Add(1);
}
