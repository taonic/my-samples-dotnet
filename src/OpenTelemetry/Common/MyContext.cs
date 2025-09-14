namespace TemporalioSamples.OpenTelemetry.Common;

public static class MyContext
{
    public static readonly AsyncLocal<Dictionary<string, string>> Metadata = new ();
}
