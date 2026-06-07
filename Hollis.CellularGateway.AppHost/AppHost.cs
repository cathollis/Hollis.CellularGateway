var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Hollis_CellularGateway_WebApi>("hollis-cellulargateway-webapi");

builder.Build().Run();
