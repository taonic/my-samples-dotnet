namespace TemporalioSamples.ActivityQueueSegregation.ActivityWorker2;

using Temporalio.Client;
using Temporalio.Worker;
using TemporalioSamples.ActivityQueueSegregation;

public sealed class Worker
{
    public const string TaskQueue = "my-activity-task-queue-2";

    public static async Task Main(string[] args)
    {
        // Create a client to localhost on "default" namespace
        var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

        // Cancellation token to shutdown worker on ctrl+c
        using var tokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            tokenSource.Cancel();
            eventArgs.Cancel = true;
        };

        // Create an activity instance with some state
        var activities = new InstanceActivities();

        // Create worker
        using var worker = new TemporalWorker(
            client,
            new TemporalWorkerOptions(TaskQueue)
                .AddActivity(activities.SelectFromDatabaseAsync));

        // Run worker until cancelled
        Console.WriteLine("Running worker");
        try
        {
            await worker.ExecuteAsync(tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Worker cancelled");
        }
    }
}