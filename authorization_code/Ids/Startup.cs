// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using IdentityServerHost.Quickstart.UI;
using Ids.DbContexts;
using Ids.Extensions;
using Ids.Service;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Ids
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddScoped<IProfileService, ProfileService>();
            // cookie policy to deal with temporary browser incompatibilities
            services.AddSameSiteCookiePolicy();
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var connectionString = Configuration.GetConnectionString("IdentityServerConnection");
            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
                options.EmitScopesAsSpaceDelimitedStringInJwt = true;

                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = true;
            })
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()//注入自定义用户登录验证
            .AddProfileService<ProfileService>(); 
            #region in-memory
            // in-memory, code config
            //builder.AddInMemoryIdentityResources(Config.IdentityResources);
            //builder.AddInMemoryApiScopes(Config.ApiScopes);
            //builder.AddInMemoryClients(Config.Clients);
            //builder.AddInMemoryApiResources(Config.ApiResources);
            #endregion
            // not recommended for production - you need to store your key material somewhere secure
            builder.AddSigningCredential();

            services.AddDbContext<IdentityServerUserDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("IdentityServerUserConnection"));
            });
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<IdentityServerUserDbContext>()
            .AddDefaultTokenProviders();

            services.AddCertificateForwardingForNginx();
            services.AddAuthentication()
                    .AddCertificate(options =>
                    {
                        options.AllowedCertificateTypes = CertificateTypes.All;
                        options.RevocationMode = X509RevocationMode.NoCheck;
                    });
            services.AddCors(options =>
            {
                // this defines a CORS policy called "default"
                options.AddPolicy("default", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            services.ConfigureNonBreakingSameSiteCookies();
        }

        public void Configure(IApplicationBuilder app)
        {
            // this will do the initial DB population
            InitializeDatabase(app);
            InitializeUserDatabase(app);
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCertificateForwarding();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseCors("default");
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
        private void InitializeDatabase(IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();
            if (!context.Clients.Any())
            {
                foreach (var client in Config.Clients)
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in Config.IdentityResources)
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiScopes.Any())
            {
                foreach (var resource in Config.ApiScopes)
                {
                    context.ApiScopes.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiResources.Any())
            {
                foreach (var resource in Config.ApiResources)
                {
                    context.ApiResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }
        }
        private void InitializeUserDatabase(IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var context = serviceScope.ServiceProvider.GetService<IdentityServerUserDbContext>();
            context.Database.Migrate();
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var identityUser = userManager.FindByNameAsync("kai").Result;
            if (identityUser != null) return;
            identityUser = new IdentityUser
            {
                UserName = "kai",
                Email = "zhangkai@163.com"
            };
            var result = userManager.CreateAsync(identityUser, "123456").Result;
            if (!result.Succeeded)  throw new Exception(result.Errors.First().Description);
            result = userManager.AddClaimsAsync(identityUser, new Claim[] {
                new Claim(JwtClaimTypes.Name, "kai"),
                new Claim(JwtClaimTypes.GivenName, "kai"),
                new Claim(JwtClaimTypes.FamilyName, "kai"),
                new Claim(JwtClaimTypes.Email, "zhangkai@163.com"),
                new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
            }).Result;

            if (!result.Succeeded) throw new Exception(result.Errors.First().Description);
        }
    }

    public static class BuilderExtensions
    {
        public static IIdentityServerBuilder AddSigningCredential(this IIdentityServerBuilder builder)
        {
            // create random RS256 key
            //builder.AddDeveloperSigningCredential();

            // use an RSA-based certificate with RS256
            //var rsaCert = new X509Certificate2("./keys/identityserver.test.rsa.p12", "changeit");
            //builder.AddSigningCredential(rsaCert, "RS256");

            // ...and PS256
            //builder.AddSigningCredential(rsaCert, "PS256");

            return builder.AddSigningCredential(new X509Certificate2("ids4.pfx", "123456"));
        }

        public static void AddCertificateForwardingForNginx(this IServiceCollection services)
        {
            services.AddCertificateForwarding(options =>
            {
                options.CertificateHeader = "X-SSL-CERT";

                options.HeaderConverter = (headerValue) =>
                {
                    X509Certificate2 clientCertificate = null;

                    if (!string.IsNullOrWhiteSpace(headerValue))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(Uri.UnescapeDataString(headerValue));
                        clientCertificate = new X509Certificate2(bytes);
                    }

                    return clientCertificate;
                };
            });
        }
    }
}