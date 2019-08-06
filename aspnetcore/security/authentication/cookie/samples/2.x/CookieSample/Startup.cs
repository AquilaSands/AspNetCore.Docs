using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CookieSample
{
    public class Startup
    {
        private readonly ILogger _logger;

        public Startup(ILogger<Startup> logger)
        {
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizePage("/Contact");
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            #region snippet1
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.EventsType = typeof(CustomCookieAuthenticationEvents);
                });

            services.AddScoped<CustomCookieAuthenticationEvents>();
            #endregion

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                //app.UseDeveloperExceptionPage();
                //app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.Use(async (context, next) =>
            {
                _logger.LogInformation("After UseExceptionHandler(): Pre next pipeline step: Request path {Path}", context.Request.Path);
                await next();
                _logger.LogInformation("After UseExceptionHandler(): Post next pipeline step: Request path {Path}", context.Request.Path);
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.Use(async (context, next) =>
            {
                _logger.LogInformation("Before UseAuthentication(): Pre next pipeline step: Request path {Path}", context.Request.Path);
                await next();
                _logger.LogInformation("Before UseAuthentication(): Post next pipeline step: Request path {Path}", context.Request.Path);
            });

            // Call UseAuthentication before calling UseMVC.
            #region snippet2
            app.UseAuthentication();
            #endregion

            app.Use(async (context, next) =>
            {
                _logger.LogInformation("After UseAuthentication(): Pre next pipeline step: Request path {Path}", context.Request.Path);
                await next();
                _logger.LogInformation("After UseAuthentication(): Post next pipeline step: Request path {Path}", context.Request.Path);
            });

            app.UseMvc();
        }
    }
}
