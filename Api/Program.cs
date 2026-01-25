using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using System.Text;


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


var bytes = Encoding.UTF8.GetBytes(configuration["JWT_SECRET"] ?? string.Empty);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = $"{configuration["Supabase:Url"] ?? configuration["SUPABASE_URL"]}/.well-known/openid-configuration";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{configuration["Supabase:Url"] ?? configuration["SUPABASE_URL"]}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "[authenticated]",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 }, // important!
            IssuerSigningKey = new SymmetricSecurityKey(bytes)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated for: " + context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    }   
    );

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Build().Run();
