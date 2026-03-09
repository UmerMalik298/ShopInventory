using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopInventory.Application.Interfaces;
using ShopInventory.Infrastructure.Services;
using ShopInventory.Infrastructure.Configuration;
namespace ShopInventory.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {



            services.AddScoped<IProductService, ProductService>();
            return services;
        }
    }
}
