using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobFlow.API.Filters;

public class RemoveCancellationTokenOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null) return;

        var toRemove = operation.Parameters
            .Where(p => string.Equals(p.Name, "cancellationToken", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var p in toRemove)
            operation.Parameters.Remove(p);
    }
}
