namespace TemporalioSamples.ActivityQueueSegregation.ActivityWorker2;

using Temporalio.Activities;

public class InstanceActivities
{
    private readonly MyDatabaseClient dbClient = new ();

    // Activities can be methods that can access state
    [Activity]
    public Task<string> SelectFromDatabaseAsync(string table) =>
        dbClient.SelectValueAsync(table);

    public class MyDatabaseClient
    {
        public Task<string> SelectValueAsync(string table) =>
            Task.FromResult($"some-db-value from table {table}");
    }
}
