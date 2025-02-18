namespace TemporalioSamples.ActivityQueueSegregation.ActivityWorker1;

using Temporalio.Activities;

public static class StaticActivities
{
    [Activity]
    public static string DoStaticThing() => "some-static-value";
}