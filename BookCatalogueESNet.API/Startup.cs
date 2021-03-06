using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookCatalogueESNet.Contracts.ElasticSearch.Services;
using BookCatalogueESNet.ElasticSearch;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookCatalogueESNet.API
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
            services.AddControllers();
            
            services.AddAutoMapper(typeof(Startup));

            AddElasticSearch(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IBookElasticService bookService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                bookService.InitClient();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        private void AddElasticSearch(IServiceCollection services)
        {
            var settings = new ConnectionConfiguration(new Uri(Configuration["ElasticSearchUrl"]));
            services.AddSingleton<IElasticLowLevelClient>(new ElasticLowLevelClient(settings));
            services.AddScoped<IBookElasticService, BookElasticService>();
        }
    }
}