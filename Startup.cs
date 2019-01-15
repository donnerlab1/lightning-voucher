using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightningVoucher.ln;
using LightningVoucher.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace LightningVoucher
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
            //var connection = "Data Source=vouchers.db";
            //services.AddDbContext<VoucherContext>(options => options.UseSqlite(connection));
           
            
           // var lightning = new LndGrpcService(rpc, cert, hex);

           
            services.AddDbContext<VoucherContext>(opt =>
                opt.UseInMemoryDatabase("VoucherList"));
            services.AddSingleton<ILightning, LndGrpcService>();
           // services.AddTransient(services.Configure<LndGrpcService>(Configuration), typeof(ILightning));
            //services.AddTransient<ILightning, >();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
