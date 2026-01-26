using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supabase;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var configuration = builder.Configuration;
var supabaseUrl = configuration["Supabase:Url"] ?? configuration["SUPABASE_URL"] ?? string.Empty;
var supabaseKey = configuration["Supabase:Key"] ?? configuration["SUPABASE_KEY"] ?? string.Empty;

builder.Services.AddTransient<Client>(sp =>
{
    var options = new SupabaseOptions
    {
        AutoConnectRealtime = true
    };
    return new Client(supabaseUrl, supabaseKey, options);
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Build().Run();
