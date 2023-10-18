using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace TNRD.Zeepkist.GTR.Auth;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog((context, configuration) =>
            {
                configuration
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Source", "Auth")
                    .MinimumLevel.Debug()
                    .WriteTo.Seq(context.Configuration["Seq:Url"], apiKey: context.Configuration["Seq:Key"])
                    .WriteTo.Console(LogEventLevel.Debug);
            })
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
