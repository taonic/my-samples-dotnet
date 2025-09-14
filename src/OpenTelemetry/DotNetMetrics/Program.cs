﻿using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Extensions.DiagnosticSource;
using Temporalio.Extensions.OpenTelemetry;
using Temporalio.Runtime;
using Temporalio.Worker;
using TemporalioSamples.OpenTelemetry.Common;

var assemblyName = typeof(TemporalClient).Assembly.GetName();

using var meter = new Meter(assemblyName.Name!, assemblyName.Version!.ToString());

var instanceId = args.ElementAtOrDefault(0) ?? throw new ArgumentException("Must pass 'worker' or 'workflow' as the single argument");

var resourceBuilder = ResourceBuilder.
    CreateDefault().
    AddService("TemporalioSamples.OpenTelemetry", serviceInstanceId: instanceId);

using var tracerProvider = Sdk.
    CreateTracerProviderBuilder().
    SetResourceBuilder(resourceBuilder).
    AddSource(TracingInterceptor.ClientSource.Name, TracingInterceptor.WorkflowsSource.Name, TracingInterceptor.ActivitiesSource.Name).
    AddOtlpExporter().
    Build();

using var meterProvider = Sdk.
    CreateMeterProviderBuilder().
    SetResourceBuilder(resourceBuilder).
    AddMeter(assemblyName.Name!).
    AddOtlpExporter().
    Build();

// Create a client to localhost on default namespace
var client = await TemporalClient.ConnectAsync(new ("localhost:7233")
{
    LoggerFactory = LoggerFactory.Create(builder =>
        builder.
            AddSimpleConsole(options => options.TimestampFormat = "[HH:mm:ss] ").
            SetMinimumLevel(LogLevel.Information)),
    Interceptors =
    [
        new TracingInterceptor(),
        new ContextPropagationInterceptor<Dictionary<string, string>>(MyContext.Metadata, DataConverter.Default.PayloadConverter),
    ],
    Runtime = new TemporalRuntime(new TemporalRuntimeOptions()
    {
        Telemetry = new TelemetryOptions()
        {
            Metrics = new MetricsOptions()
            {
                CustomMetricMeter = new CustomMetricMeter(meter),
            },
        },
    }),
});

async Task RunWorkerAsync()
{
    // Cancellation token cancelled on ctrl+c
    using var tokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        tokenSource.Cancel();
        eventArgs.Cancel = true;
    };

    // Run worker until cancelled
    Console.WriteLine("Running worker");
    using var worker = new TemporalWorker(
        client,
        new TemporalWorkerOptions(taskQueue: "opentelemetry-sample-dotnet-metrics").
            AddWorkflow<MyWorkflow>().
            AddActivity(Activities.MyActivity));
    try
    {
        await worker.ExecuteAsync(tokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Worker cancelled");
    }
}

async Task ExecuteWorkflowAsync()
{
    // Assign custom tags to AsyncLocal that will be used to tag diagnostic activity spans
    MyContext.Metadata.Value = new ()
    {
        { "userId", "alice-123" },
        { "accountId", "account-456" },
    };

    Console.WriteLine("Executing workflow");
    await client.ExecuteWorkflowAsync(
        (MyWorkflow wf) => wf.RunAsync(),
        new (id: "opentelemetry-sample-dotnet-workflow-id", taskQueue: "opentelemetry-sample-dotnet-metrics"));
}

switch (args.ElementAtOrDefault(0))
{
    case "worker":
        await RunWorkerAsync();
        break;
    case "workflow":
        await ExecuteWorkflowAsync();
        break;
    default:
        throw new ArgumentException("Must pass 'worker' or 'workflow' as the single argument");
}
