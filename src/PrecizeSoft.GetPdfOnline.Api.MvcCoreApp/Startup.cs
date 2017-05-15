﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrecizeSoft.GetPdfOnline.Data.SQLite;
using PrecizeSoft.GetPdfOnline.Data.SQLite.Repositories;
using PrecizeSoft.GetPdfOnline.Data;
using PrecizeSoft.GetPdfOnline.Api.MvcCoreApp.Configuration;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using System.ServiceModel;
using PrecizeSoft.GetPdfOnline.Api.Soap.Host;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using PrecizeSoft.GetPdfOnline.Domain.Configuration;
using PrecizeSoft.GetPdfOnline.Api.MvcCoreApp.Swagger;
using Microsoft.Data.Sqlite;
using PrecizeSoft.IO.Contracts.Converters;
using PrecizeSoft.GetPdfOnline.Domain.Services;

namespace PrecizeSoft.GetPdfOnline.Api.MvcCoreApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("usersettings.txt", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        private SqliteConnection cacheConnection = null;

        SoapApiHost soapApiHost = null;

        protected void CreateAndOpenHosts(int port, bool useLibreOfficeCustomPath, string libreOfficeCustomPath,
            string connectionString)
        {
            this.soapApiHost = new SoapApiHost();
            this.soapApiHost.Configure(false, port, "/soap", useLibreOfficeCustomPath, libreOfficeCustomPath, connectionString);
            this.soapApiHost.Open();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            cacheConnection = new SqliteConnection("Data Source=:memory:");
            cacheConnection.Open();

            services.AddDbContext<CacheDbContext>(options =>
                options.UseSqlite(cacheConnection));

            services.AddDbContext<GetPdfOnlineDbContext>(options =>
                options.UseSqlite(this.Configuration.Get<UserSettingsOptions>().Data.ConnectionString));

            services.AddTransient<ICacheRepository, CacheRepository>();
            services.AddTransient<IConvertLogRepository, ConvertLogRepository>();
            services.AddTransient<IFileService, FileStorageService>(p =>
            {
                return new FileStorageService(p.GetRequiredService<ICacheRepository>());
            });
            services.AddTransient<IJobService, JobService>(p =>
            {
                return new JobService(p.GetRequiredService<ICacheRepository>());
            });

            // Add the localization services to the services container
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            /*services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });*/

            // Add framework services.
            services.AddMvc()
                // Add support for finding localized views, based on file name suffix, e.g. Index.fr.cshtml
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                // Add support for localizing strings in data annotations (e.g. validation messages) via the
                // IStringLocalizer abstractions.
                .AddDataAnnotationsLocalization();

            services.Configure<UserSettingsOptions>(Configuration);
            services.Configure<TitleOptions>(Configuration.GetSection("View:Title"));
            services.Configure<LibreOfficeOptions>(Configuration.GetSection("LibreOffice"));
            services.Configure<StoreOptions>(Configuration.GetSection("View:Cache"));

            // Configure supported cultures and localization options
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("ru")
                };

                // State what the default culture for your application is. This will be used if no specific culture
                // can be determined for a given request.
                options.DefaultRequestCulture = new RequestCulture(culture: "en", uiCulture: "en");

                // You must explicitly state which cultures your application supports.
                // These are the cultures the app supports for formatting numbers, dates, etc.
                options.SupportedCultures = supportedCultures;

                // These are the cultures the app supports for UI strings, i.e. we have localized resources for.
                options.SupportedUICultures = supportedCultures;

                // You can change which providers are configured to determine the culture for requests, or even add a custom
                // provider with your own logic. The providers will be asked in order to provide a culture for each request,
                // and the first to provide a non-null result that is in the configured supported cultures list will be used.
                // By default, the following built-in providers are configured:
                // - QueryStringRequestCultureProvider, sets culture via "culture" and "ui-culture" query string values, useful for testing
                // - CookieRequestCultureProvider, sets culture via "ASPNET_CULTURE" cookie
                // - AcceptLanguageHeaderRequestCultureProvider, sets culture via the "Accept-Language" request header
                //options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
                //{
                //  // My custom request culture logic
                //  return new ProviderCultureResult("en");
                //}));
            });

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Rest API",
                    Version = "v1",
                    Contact = new Contact { Name = "Mikhail Andreev", Url = "http://andreev.work", Email = "m@andreev.work" }
                });

                //Set the comments path for the swagger json and ui.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                c.IncludeXmlComments(Path.Combine(basePath, "PrecizeSoft.IO.WebApi.xml"));
                c.IncludeXmlComments(Path.Combine(basePath, "PrecizeSoft.GetPdfOnline.Api.MvcCoreApp.xml"));

                c.OrderActionsBy(apiDescription =>
                {
                    return $"{apiDescription.GroupName}!{apiDescription.RelativePath}!{apiDescription.HttpMethod}!";
                });

                c.OperationFilter<FormFileOperationFilter>();
                c.OperationFilter<UpdateFileResponseTypeFilter>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IApplicationLifetime life, CacheDbContext cacheDbContext, GetPdfOnlineDbContext getPdfOnlineDbContext)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            //app.UseCors("AllowSpecificOrigin");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            cacheDbContext.Database.EnsureCreated();
            getPdfOnlineDbContext.Database.Migrate();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "rest/{documentName}/swagger.json";
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            /*app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/rest/v1/swagger.json", "GetPDF.online REST API v1");
                c.RoutePrefix = "swagger";
            });*/

            life.ApplicationStarted.Register(() =>
            {
                var options = this.Configuration.Get<UserSettingsOptions>();
                this.CreateAndOpenHosts(options.Host.TcpPort, options.LibreOffice.UseCustomUnoPath, options.LibreOffice.CustomUnoPath, options.Data.ConnectionString);
            });
        }
    }
}