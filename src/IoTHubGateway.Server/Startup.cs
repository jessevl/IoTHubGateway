using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IoTHubGateway.Server.Services;

namespace IoTHubGateway.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();

            // A single instance of registered devices must be kept
            services.AddSingleton<RegisteredDevices>();

            // Resolve server options
            var options = new ServerOptions();
            Configuration.GetSection(nameof(ServerOptions)).Bind(options);
            services.AddSingleton<ServerOptions>(options);

            options.IoTHubHostName = Environment.GetEnvironmentVariable("IoTHubHostName");

     

#if DEBUG
            SetupDebugListeners(options);
#endif

            // A single instance of gateway implementation must be kept
            services.AddSingleton<IGatewayService, GatewayService>();

            services.AddControllers();
        }

#if DEBUG
        private void SetupDebugListeners(ServerOptions options)
        {
           if (options.DirectMethodEnabled && options.DirectMethodCallback == null)
           {
               options.DirectMethodCallback = (methodRequest, userContext) =>
               {
                   var deviceId = (string)userContext;
                   Console.WriteLine($"[{DateTime.Now.ToString()}] Device method for {deviceId}.{methodRequest.Name}({methodRequest.DataAsJson}) received");

                   var responseBody = "{ succeeded: true }";
                   MethodResponse methodResponse = new MethodResponse(Encoding.UTF8.GetBytes(responseBody), 200);

                   return Task.FromResult(methodResponse);
               };
           }

         
        }
#endif

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
     

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
