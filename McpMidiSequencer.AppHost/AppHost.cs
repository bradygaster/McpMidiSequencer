var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.McpServer>("mcpserver");

builder.Build().Run();
