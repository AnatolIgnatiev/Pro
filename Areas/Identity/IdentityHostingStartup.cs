using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pro.Areas.Identity.Data;
using Pro.Data;

[assembly: HostingStartup(typeof(Pro.Areas.Identity.IdentityHostingStartup))]
namespace Pro.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<ProContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("ProContextConnection")));

                services.AddDefaultIdentity<ProUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<ProContext>();
            });
        }
    }
}