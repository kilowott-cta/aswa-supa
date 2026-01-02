using BlazorBasic;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var configuration = builder.Configuration;
var supabaseUrl = configuration["Supabase:Url"] ?? configuration["SUPABASE_URL"] ?? string.Empty;
var supabaseKey = configuration["Supabase:Key"] ?? configuration["SUPABASE_KEY"] ?? string.Empty;

var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true,
};

var supabase = new Supabase.Client(supabaseUrl, supabaseKey, options);
builder.Services.AddSingleton(supabase);

await builder.Build().RunAsync();
