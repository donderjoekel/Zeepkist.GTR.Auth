using System;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SteamWebAPI2.Utilities;
using TNRD.Zeepkist.GTR.Auth.Database;
using TNRD.Zeepkist.GTR.Auth.Directus;
using TNRD.Zeepkist.GTR.Auth.Directus.Options;
using TNRD.Zeepkist.GTR.Auth.Jwt;
using TNRD.Zeepkist.GTR.Auth.Options;

namespace TNRD.Zeepkist.GTR.Auth
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
            services.Configure<AuthOptions>(Configuration.GetSection("Auth"));
            services.Configure<DirectusOptions>(Configuration.GetSection("Directus"));
            services.Configure<SteamOptions>(Configuration.GetSection("Steam"));

            services.AddHttpClient("directus",
                (provider, client) =>
                {
                    DirectusOptions options = provider.GetRequiredService<IOptions<DirectusOptions>>().Value;

                    string baseUrl = $"http://{options.BaseUrl}:{options.Port}";

                    client.BaseAddress = new Uri(baseUrl);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", options.Token);
                });
            
            services.AddNpgsql<GTRContext>(Configuration["Database:ConnectionString"]);

            services.AddSingleton<IDirectusClient, DirectusClient>();
            services.AddSingleton<SteamWebInterfaceFactory>(provider =>
            {
                SteamOptions steamOptions = provider.GetRequiredService<IOptions<SteamOptions>>().Value;
                return new SteamWebInterfaceFactory(steamOptions.Token);
            });

            services.AddScoped<ExternalTokenService>();
            services.AddScoped<GameTokenService>();

            services.AddAuthentication(opts =>
                {
                    opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(opts =>
                {
                    opts.LoginPath = "/login";
                    opts.LogoutPath = "/signout";
                })
                .AddSteam(opts =>
                {
                    opts.ApplicationKey = Configuration.GetSection("Steam")!.Get<SteamOptions>()!.Token;
                    opts.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddJwtBearer(opts =>
                {
                    string tokenSigningKey = Configuration.GetSection("Auth")!.Get<AuthOptions>()!.SigningKey;
                    SecurityKey key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenSigningKey));
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = key,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(60),
                        ValidAudience = null,
                        ValidateAudience = false,
                        ValidIssuer = null,
                        ValidateIssuer = false
                    };
                    opts.TokenValidationParameters.ValidateAudience =
                        opts.TokenValidationParameters.ValidAudience is not null;
                    opts.TokenValidationParameters.ValidateIssuer =
                        opts.TokenValidationParameters.ValidIssuer is not null;
                });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ZeepkistGTR.Auth", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZeepkistGTR.Auth v1"));
            }

            app.UseCors(policyBuilder => policyBuilder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
