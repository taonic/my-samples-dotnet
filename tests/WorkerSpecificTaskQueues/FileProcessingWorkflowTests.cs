namespace TemporalioSamples.Tests.WorkerSpecificTaskQueues;

using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Worker;
using TemporalioSamples.WorkerSpecificTaskQueues;
using Xunit;
using Xunit.Abstractions;

public class FileProcessingWorkflowTests : WorkflowEnvironmentTestBase
{
    public FileProcessingWorkflowTests(ITestOutputHelper output, WorkflowEnvironment env)
        : base(output, env)
    {
    }

    [Fact]
    public async Task RunAsync_SimpleRun_SucceedsAfterRetry()
    {
        var taskQueue = $"tq-{Guid.NewGuid()}";
        var workerTaskQueue = $"worker-{Guid.NewGuid()}";

        // Mock activities
        [Activity("GetUniqueTaskQueue")]
        string MockGetUniqueTaskQueue() => workerTaskQueue;

        [Activity("DownloadFileToWorkerFileSystem")]
        string MockDownloadFileToWorkerFileSystem() => "/path/to/file";

        // We want this to fail the first two times
        var timesCalled = 0;
        [Activity("WorkOnFileInWorkerFileSystem")]
        void MockWorkOnFileInWorkerFileSystem(string path)
        {
            timesCalled++;
            if (timesCalled < 3)
            {
                throw new InvalidOperationException("Intentional failure");
            }
        }

        [Activity("CleanupFileFromWorkerFileSystem")]
        void MockCleanupFileFromWorkerFileSystem(string path)
        {
        }

        using var mainWorker = new TemporalWorker(Client,
            new TemporalWorkerOptions("workflowTaskQueue").AddWorkflow<FileProcessingWorkflow>());

        using var activityWorker1 = new TemporalWorker(Client,
            new TemporalWorkerOptions("activityTaskQueue1").AddActivity(MockActivity1));

        using var activityWorker2 = new TemporalWorker(Client,
            new TemporalWorkerOptions("activityTaskQueue2").AddActivity(MockActivity2));

        var tcs = new TaskCompletionSource();
        await Task.WhenAll(
            mainWorker.ExecuteAsync(async () =>
            {
                await Client.ExecuteWorkflowAsync(
                    (FileProcessingWorkflow wf) => wf.RunAsync(5),
                    new(id: $"wf-{Guid.NewGuid()}", taskQueue: taskQueue));
                Assert.Equal(3, timesCalled);
                tcs.SetResult();
            }),
            activityWorker1.ExecuteAsync(() => tcs.Task),
            activityWorker2.ExecuteAsync(() => tcs.Task)
        );
    }
}
