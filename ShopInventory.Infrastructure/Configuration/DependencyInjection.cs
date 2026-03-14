using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopInventory.Application.Interfaces;
using ShopInventory.Application.Services;
using ShopInventory.Infrastructure.Configuration;
using ShopInventory.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace ShopInventory.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {



            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductVariantService, ProductVariantService>();
            services.AddScoped<ISyncService, SyncService>();
            services.AddScoped<ISaleService, SaleService>();
            services.AddScoped<IDashboardService, DashboardService>();


            return services;
        }
    }
}
