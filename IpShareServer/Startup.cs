﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using IpShareServer.Helpers;
using IpShareServer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace IpShareServer
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
           

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddHostedService <GetEphemerides>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.Extensions.Hosting.IHostingEnvironment env)
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
            app.UseWebSockets();

          
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        Console.WriteLine($"New Connection {context.Connection.RemoteIpAddress}");
                        var message = WebSocketHelper.GetMessage(webSocket).Result;
                        var messageMass = message.Split(":");
                        var user = new Models.User(webSocket,  Guid.NewGuid());
                        if (messageMass[0] == "State")
                        {
                            if (messageMass[1] == "Matlab")
                            {
                                user._state = "Matlab";
                                Program.MatlabUser.Add(user);
                                Console.WriteLine($"New Connected MatlabUser {context.Connection.RemoteIpAddress} " + user._guid);
                            }
                            else if(messageMass[1] == "MainMatlabUser")
                            {
                                user._state = "Matlab";
                                Program.MainMatlabUser = user;
                                Console.WriteLine($"New Connected MainMatlabUser {context.Connection.RemoteIpAddress} " + user._guid);
                            }
                            else
                            {
                                user._model = messageMass[2];
                                Program.Users.Add(user);
                                Console.WriteLine($"New Connected Phone {context.Connection.RemoteIpAddress} " + user._guid +"  " + messageMass[2]);
                               //Console.WriteLine($"New Connected Phone {context.Connection.RemoteIpAddress} " + user._guid);
                            }
                            await user.Echo();
                        }
                        else
                        {
                            await user.Echo();
                        }
                    }

                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                  
                }                          
                else
                {
                    await next();
                }
            });
            app.UseMvc();
        }
    }
}
