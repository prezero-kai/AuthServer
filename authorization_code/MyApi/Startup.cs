using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MyApi
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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        //IdentityServer地址
                        options.Authority = "https://localhost:5001";
                        //对应ApiResource的Name
                        options.Audience = "api";
                        //不使用https
                        options.RequireHttpsMetadata = false;
                        IdentityModelEventSource.ShowPII = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            //是否秘钥认证
                            ValidateIssuerSigningKey = true,
                            //是否验证发行人
                            ValidateIssuer = true,
                            //是否验证订阅
                            ValidateAudience = true,

                            //是否验证过期时间
                            RequireExpirationTime = true,
                            ValidateLifetime = true
                        };
                    });
            services.AddAuthorization(options =>
            {
                //基于策略授权
                options.AddPolicy("WeatherPolicy", builder =>
                {
                    //客户端Scope中包含api1.weather.scope才能访问
                    builder.RequireScope("api.weather.scope");
                });
                //基于策略授权
                options.AddPolicy("TestPolicy", builder =>
                {
                    //客户端Scope中包含api1.test.scope才能访问
                    builder.RequireScope("api.test.scope");
                });
            });

            services.AddCors(options =>
            {
                // this defines a CORS policy called "default"
                options.AddPolicy("default", policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("default");

            //身份验证
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
