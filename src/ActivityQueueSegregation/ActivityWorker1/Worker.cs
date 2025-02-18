namespace TemporalioSamples.ActivityQueueSegregation.ActivityWorker1;

using Temporalio.Client;
using Temporalio.Worker;
using TemporalioSamples.ActivityQueueSegregation;

public sealed class Worker
{
    public const string TaskQueue = "my-activity-task-queue-1";

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

        // Create worker
        using var worker = new TemporalWorker(
            client,
            new TemporalWorkerOptions(TaskQueue)
                .AddActivity(StaticActivities.DoStaticThing));

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