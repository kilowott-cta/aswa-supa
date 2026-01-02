using BlazorBasic;
using BlazorBasic.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuration = builder.Configuration;
var supabaseUrl = configuration["Supabase:Url"] ?? configuration["SUPABASE_URL"] ?? string.Empty;
var supabaseKey = configuration["Supabase:Key"] ?? configuration["SUPABASE_KEY"] ?? string.Empty;

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthenticationStateProvider>();

var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true,
};

builder.Services.AddHttpClient<Supabase.Client>("SupabaseClient", client =>
{
    if (!string.IsNullOrEmpty(supabaseUrl))
	{
		// PostgREST endpoint for tables is under /rest/v1
		client.BaseAddress = new Uri(supabaseUrl);
	}
	if (!string.IsNullOrEmpty(supabaseKey))
	{
		client.DefaultRequestHeaders.Add("apikey", supabaseKey);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
	}
	client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });


await builder.Build().RunAsync();
