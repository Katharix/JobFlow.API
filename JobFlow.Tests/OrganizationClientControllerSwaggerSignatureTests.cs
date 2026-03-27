using JobFlow.API.Controllers;
using JobFlow.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.Tests;

public class OrganizationClientControllerSwaggerSignatureTests
{
    [Fact]
    public void PreviewClientImport_UsesFromFormRequestDto_AndConsumesMultipartFormData()
    {
        var method = typeof(OrganizationClientController)
            .GetMethod(nameof(OrganizationClientController.PreviewClientImport));

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.NotEmpty(parameters);

        var requestParam = parameters[0];
        Assert.Equal(typeof(PreviewClientImportRequest), requestParam.ParameterType);
        Assert.NotNull(requestParam.GetCustomAttributes(typeof(FromFormAttribute), inherit: true).SingleOrDefault());

        Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(IFormFile));

        var consumes = method.GetCustomAttributes(typeof(ConsumesAttribute), inherit: true)
            .Cast<ConsumesAttribute>()
            .SingleOrDefault();

        Assert.NotNull(consumes);
        Assert.Contains("multipart/form-data", consumes!.ContentTypes);
    }
}
