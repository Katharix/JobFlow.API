using System.Reflection;

var asm = typeof(Microsoft.OpenApi.OpenApiConstants).Assembly;
Console.WriteLine(asm.FullName);

var hasModelsNs = asm.GetTypes().Any(t => t.Namespace == "Microsoft.OpenApi.Models");
Console.WriteLine($"Has Microsoft.OpenApi.Models namespace: {hasModelsNs}");

var openApiInfo = asm.GetType("Microsoft.OpenApi.Models.OpenApiInfo");
Console.WriteLine($"OpenApiInfo type: {openApiInfo?.FullName ?? "<null>"}");
