# Activity Queue Segregation Sample

This sample demonstrates how to run different activities on separate task queues in Temporal .NET SDK.

## Components

- `ActivityWorker1`: Hosts static activities
- `ActivityWorker2`: Hosts instance activities with database operations
- `WorkflowWorker`: Runs the workflow that coordinates activities
- `Client`: Initiates the workflow

## Running the Sample

1. Start a local Temporal server
2. Run the workers in separate terminals:
   ```bash
   dotnet run --project src/ActivityQueueSegregation/ActivityWorker1
   dotnet run --project src/ActivityQueueSegregation/ActivityWorker2
   dotnet run --project src/ActivityQueueSegregation/WorkflowWorker
   ```
3. Execute the workflow:
   ```bash
   dotnet run --project src/ActivityQueueSegregation/Client
   ```

## Key Concepts

- Activities can be distributed across different task queues
- Different worker types (static/instance) can be isolated
- Task queues provide natural boundaries for scaling and deployment