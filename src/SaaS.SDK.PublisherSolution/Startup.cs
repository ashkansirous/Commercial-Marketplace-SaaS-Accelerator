﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.

using System;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Marketplace.Metering;
using Microsoft.Marketplace.SaaS;
using Microsoft.Marketplace.SaaS.SDK.Services.Configurations;
using Microsoft.Marketplace.SaaS.SDK.Services.Contracts;
using Microsoft.Marketplace.SaaS.SDK.Services.Models;
using Microsoft.Marketplace.SaaS.SDK.Services.Services;
using Microsoft.Marketplace.SaaS.SDK.Services.Utilities;
using Microsoft.Marketplace.SaasKit.Client.DataAccess.Contracts;
using Microsoft.Marketplace.SaasKit.Client.DataAccess.Services;

namespace Microsoft.Marketplace.Saas.Web;

/// <summary>
///     Startup.
/// </summary>
public class Startup
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Startup" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    ///     Gets the configuration.
    /// </summary>
    /// <value>
    ///     The configuration.
    /// </value>
    public IConfiguration Configuration { get; }

    /// <summary>
    ///     Configures the services.
    /// </summary>
    /// <param name="services">The services.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole();
        });

        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        var config = new SaaSApiClientConfiguration
        {
            AdAuthenticationEndPoint = Configuration["SaaSApiConfiguration:AdAuthenticationEndPoint"],
            ClientId = Configuration["SaaSApiConfiguration:ClientId"],
            ClientSecret = Configuration["SaaSApiConfiguration:ClientSecret"],
            FulFillmentAPIBaseURL = Configuration["SaaSApiConfiguration:FulFillmentAPIBaseURL"],
            MTClientId = Configuration["SaaSApiConfiguration:MTClientId"],
            FulFillmentAPIVersion = Configuration["SaaSApiConfiguration:FulFillmentAPIVersion"],
            GrantType = Configuration["SaaSApiConfiguration:GrantType"],
            Resource = Configuration["SaaSApiConfiguration:Resource"],
            SaaSAppUrl = Configuration["SaaSApiConfiguration:SaaSAppUrl"],
            SignedOutRedirectUri = Configuration["SaaSApiConfiguration:SignedOutRedirectUri"],
            TenantId = Configuration["SaaSApiConfiguration:TenantId"],
            SupportMeteredBilling = Convert.ToBoolean(Configuration["SaaSApiConfiguration:supportmeteredbilling"])
        };
        var knownUsers = new KnownUsersModel
        {
            KnownUsers = Configuration["KnownUsers"]
        };
        var creds = new ClientSecretCredential(config.TenantId, config.ClientId, config.ClientSecret);


        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = $"{config.AdAuthenticationEndPoint}/common/v2.0";
                options.ClientId = config.MTClientId;
                options.ResponseType = OpenIdConnectResponseType.IdToken;
                options.CallbackPath = "/Home/Index";
                options.SignedOutRedirectUri = config.SignedOutRedirectUri;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.ValidateIssuer = false;
            })
            .AddCookie();

        services
            .AddTransient<IClaimsTransformation, CustomClaimsTransformation>();

        services
            .AddSingleton<IFulfillmentApiService>(new FulfillmentApiService(new MarketplaceSaaSClient(creds), config,
                new FulfillmentApiClientLogger()))
            .AddSingleton<IMeteredBillingApiService>(new MeteredBillingApiService(new MarketplaceMeteringClient(creds),
                config, new MeteringApiClientLogger()))
            .AddSingleton(config)
            .AddSingleton(knownUsers);


        InitializeRepositoryServices(services);

        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(5);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        services.AddMvc(option => option.EnableEndpointRouting = false);
        services.AddControllersWithViews();

        services.Configure<CookieTempDataProviderOptions>(options => { options.Cookie.IsEssential = true; });
    }

    /// <summary>
    ///     Configures the specified application.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="env">The env.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseSession();
        app.UseAuthentication();
        app.UseMvc(routes =>
        {
            routes.MapRoute(
                "default",
                "{controller=Home}/{action=Index}/{id?}");
        });
    }

    /// <summary>
    ///     Initializes the repository services.
    /// </summary>
    /// <param name="services">The services.</param>
    private static void InitializeRepositoryServices(IServiceCollection services)
    {
        services.AddScoped<ISubscriptionsRepository, SubscriptionsRepository>();
        services.AddScoped<IPlansRepository, PlansRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<ISubscriptionLogRepository, SubscriptionLogRepository>();
        services.AddScoped<IApplicationConfigRepository, ApplicationConfigRepository>();
        services.AddScoped<IApplicationLogRepository, ApplicationLogRepository>();
        services.AddScoped<ISubscriptionUsageLogsRepository, SubscriptionUsageLogsRepository>();
        services.AddScoped<IMeteredDimensionsRepository, MeteredDimensionsRepository>();
        services.AddScoped<IKnownUsersRepository, KnownUsersRepository>();
        services.AddScoped<IOffersRepository, OffersRepository>();
        services.AddScoped<IValueTypesRepository, ValueTypesRepository>();
        services.AddScoped<IOfferAttributesRepository, OfferAttributesRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IPlanEventsMappingRepository, PlanEventsMappingRepository>();
        services.AddScoped<IEventsRepository, EventsRepository>();
        services.AddScoped<KnownUserAttribute>();
        services.AddScoped<IEmailService, SMTPEmailService>();
        services.AddScoped<ISchedulerFrequencyRepository, SchedulerFrequencyRepository>();
        services.AddScoped<IMeteredPlanSchedulerManagementRepository, MeteredPlanSchedulerManagementRepository>();
        services.AddScoped<ISchedulerManagerViewRepository, SchedulerManagerViewRepository>();
    }
}