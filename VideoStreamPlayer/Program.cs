using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoStreamPlayer.HttpClients;
using VideoStreamPlayer.StreamProviders;

namespace VideoStreamPlayer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var host = Host.CreateDefaultBuilder()
             .ConfigureAppConfiguration((context, builder) =>
             {
                 // configuration file added, but not used
                 // this is just example of loading configuration file for completness of my DI knowledge checking
                 builder.AddJsonFile("appsettings.local.json", optional: false);
             })
             .ConfigureServices((context, services) =>
             {
                 ConfigureServices(context.Configuration, services);
             })
             .Build();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(host.Services.GetRequiredService<MainForm>());
        }

        private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<MainForm>();
            services.AddSingleton<IStreamClient, StreamClient>();
            services.AddSingleton<WebStreamProvider>();
            services.AddSingleton<FileStreamProvider>();
        }
    }
}
