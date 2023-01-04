using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Raydreams.GMailer
{
    /// <summary>Bootstrap Class</summary>
    public static class Program
    {
        /// <summary>Main entry class</summary>
        /// <param name="args">any future command line args</param>
        /// <returns>exit value</returns>
        public static int Main( string[] args )
        {
            Console.WriteLine( "Starting..." );

            bool isMac = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            // get the environment var
            string env = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" ) ?? "Development";

            // inject all the input
            IHostBuilder builder = new HostBuilder()
            .ConfigureLogging( (ctx, logging) =>
            {
                logging.AddConfiguration( ctx.Configuration.GetSection( "Logging" ) );
                logging.ClearProviders();
                logging.AddDebug();
                logging.AddConsole();
            } )
            .ConfigureAppConfiguration( ( ctx, config ) =>
            {
                config.AddJsonFile( $"appsettings.json", false, true )
                .AddJsonFile( $"appsettings.{env}.json", true, true );
                config.AddEnvironmentVariables();

                if ( args != null )
                    config.AddCommandLine( args );
            } )
            .ConfigureServices( ( ctx, services ) =>
            {
                services.AddOptions();
                
                // get the app config file
                services.AddScoped<AppConfig>( p => {
                    return ctx.Configuration.GetSection( "AppConfig" ).Get<AppConfig>();
                } );

                // add the re-writer
                services.AddScoped<IMIMERewriter, MIMEKitRewriter>( p =>{
                    return ctx.Configuration.GetSection( "MIMERewriter" ).Get<MIMEKitRewriter>();
                } );

                // add the logger
                services.AddLogging();

                // add hosted service
                services.AddHostedService<GMailer>();
            } );

            // run the host sync
            // using just Build gives the Worker you can pass a cancellation token to
            builder.Build().Start();

            Console.WriteLine( "Stopping..." );

            return 0;
        }
    }
}

//public class GMailer2 : BackgroundService
//{
//    /// <summary></summary>
//    /// <param name="config"></param>
//    /// <param name="logger"></param>
//    public GMailer2( AppConfig config, ILogger<GMailer2> logger )
//    {
//        this.Settings = config;
//        this.Logger = logger;
//    }

//    /// <summary>The App Settings</summary>
//    protected AppConfig Settings { get; set; }

//    /// <summary></summary>
//    protected ILogger<GMailer2> Logger;

//    /// <summary></summary>
//    protected override Task<int> ExecuteAsync( CancellationToken stoppingToken )
//    {
//        // lets do the thing
//        try
//        {
//            // setup everything
//            GMailer app = new GMailer( this.Settings )
//            {
//                Rewriter = new MIMEKitRewriter( this.Settings.SubjectPrefix )
//            };

//            // run it
//            //int res = app.Run();
//        }
//        catch ( System.Exception exp )
//        {
//            Console.WriteLine( exp.Message );
//            return Task.FromResult( -1 );
//        }

//        return Task.FromResult( 0 );
//    }
//}

//CancellationToken cts = new CancellationToken();
//Task t = svc.StartAsync(cts);

//// lets do the thing
//try
//{
//    // setup everything
//    GMailer app = new GMailer( config )
//    {
//        Rewriter = new MIMEKitRewriter( config.SubjectPrefix )
//    };

//    // run it
//    runResult = app.Run();
//}
//catch ( System.Exception exp )
//{
//    Console.WriteLine( exp.Message );
//    return -1;
//}

//return runResult;
