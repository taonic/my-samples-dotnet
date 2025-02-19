namespace TemporalioSamples.ActivityQueueSegregation.Workflow;

using Microsoft.Extensions.Logging;
using Temporalio.Workflows;
using TemporalioSamples.ActivityQueueSegregation.ActivityWorker1;

[Workflow]
public class SayHelloWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync(string name)
    {
        // Run a sync static method activity in it's own task queue.
        var result1 = await Workflow.ExecuteActivityAsync(
            () => ActivityWorker1.StaticActivities.DoStaticThing(),
            new()
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(10),
                TaskQueue = ActivityWorker1.Worker.TaskQueue,
            });
        Workflow.Logger.LogInformation("Activity static method result: {Result}", result1);

        // Run an async instance method activity in it's own task queue.
        var result2 = await Workflow.ExecuteActivityAsync<string>(
            "SelectFromDatabase", // Alternatively, you can reference the Activity by its method name.
            new object?[] { "some-db-table" },
            new()
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(10),
                TaskQueue = "my-activity-task-queue-2",
            });
        Workflow.Logger.LogInformation("Activity instance method result: {Result}", result2);

        // We'll go ahead and return this result
        return result2;
    }
}