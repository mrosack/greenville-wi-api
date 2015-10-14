﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Microsoft.Net.Http.Headers;

namespace GreenvilleWiApi.WebApi5
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
        }

        // This method gets called by a runtime.
        // Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(c =>
            {
                var jsonFormatter = c.OutputFormatters.OfType<JsonOutputFormatter>().First();
                jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                jsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                jsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            });
            
            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();

            services.AddSwagger();
            services.ConfigureSwaggerDocument(c =>
            {
                c.SingleApiVersion(new Info { Version = "v1", Title = "GreenvilleWiApi" });
                c.OperationFilter<GreenvilleApiFilter>();
            });

            services.ConfigureSwaggerSchema(c =>
            {
                c.DescribeAllEnumsAsStrings = true;
                c.ModelFilter<GreenvilleApiFilter>();
            });
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Configure the HTTP request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc();
            // Add the following route for porting Web API 2 controllers.
            // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");

            app.UseSwagger();
            app.UseSwaggerUi();
        }

        private class GreenvilleApiFilter : IModelFilter, IOperationFilter
        {
            public void Apply(Schema model, ModelFilterContext context)
            {
                foreach (var dtProp in model.Properties.Where(x => x.Value.Format == "date-time"))
                    dtProp.Value.Format = "date";
            }

            public void Apply(Operation operation, OperationFilterContext context)
            {
                foreach (var dtParam in operation.Parameters.Where(x => x is NonBodyParameter).Cast<NonBodyParameter>())
                {
                    if (dtParam.In == "modelbinding")
                        dtParam.In = "query";

                    if (dtParam.Format == "date-time")
                        dtParam.Format = "date";
                }
            }
        }
    }
}