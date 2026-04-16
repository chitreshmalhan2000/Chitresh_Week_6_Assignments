using cosmosdbdemo.Data;
using Microsoft.Azure.Cosmos;

namespace cosmosdbdemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddSingleton<CosmosDbService>(options =>
            {
                var _configuration = builder.Configuration;

                var cosmosClient = new CosmosClient(
                    _configuration["CosmosDb:Endpoint"],
                    _configuration["CosmosDb:PrimaryKey"]);

                return new CosmosDbService(
                    cosmosClient,
                    _configuration["CosmosDb:DatabaseName"],
                    _configuration["CosmosDb:ContainerName"]);
            });



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
