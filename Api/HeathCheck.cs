using System.Security.Claims;
using System.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Function.Basic;

public class HealthCheck
{
    private readonly ILogger<HealthCheck> _logger;

    public HealthCheck(ILogger<HealthCheck> logger)
    {
        _logger = logger;
    }

    [Function("healthcheck")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        ClaimsPrincipal user = req.HttpContext.User;
        _logger.LogInformation("User: {User}", user.Identity.Name);
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}