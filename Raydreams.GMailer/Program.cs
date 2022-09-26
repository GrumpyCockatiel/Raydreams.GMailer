﻿using Microsoft.Extensions.Configuration;

namespace Raydreams.GMailer
{
    /// <summary>Startup Class</summary>
    public static class Program
    {
        static int Main( string[] args )
        {
			// load the configuarion file
			IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile( $"appsettings.json", true, true )
                //.AddJsonFile( $"appsettings.{env}.json", true, true )
                .AddEnvironmentVariables();

            // init AppConfig
            IConfigurationRoot configurationRoot = builder.Build();
            AppConfig config = configurationRoot.GetSection( nameof( AppConfig ) ).Get<AppConfig>();

            int runResult = 0;

            try
            {
                GMailer app = new GMailer( config );
                runResult = app.Run();
            }
            catch ( System.Exception exp )
            {
                Console.WriteLine( exp.Message );
                return -1;
            }

            return runResult;
        }
    }
}
