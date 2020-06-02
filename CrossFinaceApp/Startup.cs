using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CrossFinaceApp.DataAccess;
using CrossFinaceApp.Models;
using FluentValidation.AspNetCore;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CrossFinaceApp
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
            services.AddControllers()
                .AddFluentValidation(opt =>
                {
                    opt.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
                });

            services.AddDbContext<DataContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Default")));

            services.AddMediatR(Assembly.GetExecutingAssembly());

            services.AddScoped<ImportService.IImportService, ImportService.ImportServiceClient>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ConfigureMappers();

            app.UseHttpsRedirection();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static void ConfigureMappers()
        {
            TypeAdapterConfig<Address, ImportService.Address>
                .NewConfig()
                .Map(dest => dest.Street, src => src.StreetName)
                .Map(dest => dest.HouseNo, src => src.StreetNumber)
                .Map(dest => dest.City, src => src.PostOfficeCity)
                .Map(dest => dest.PostalCode, src => src.PostCode)
                .Map(dest => dest.LocaleNo, src => src.FlatNumber);

            TypeAdapterConfig<FinancialState, ImportService.FinancialState>
                .NewConfig()
                .Map(dest => dest.CourtRepresentationFees, src => src.RepresentationCourtFees)
                .Map(dest => dest.CourtFees, src => src.CourtFees)
                .Map(dest => dest.Capital, src => src.OutstandingLiabilites)
                .Map(dest => dest.Interest, src => src.Interests)
                .Map(dest => dest.PenaltyInterest, src => src.PenaltyInterests)
                .Map(dest => dest.Fees, src => src.Fees);

            TypeAdapterConfig<Person, ImportService.Person>
                .NewConfig()
                .Map(dest => dest.Addresses, src => MapContext.Current.Parameters["Addresses"])
                .Map(dest => dest.FinancialState, src => MapContext.Current.Parameters["FinancialState"])
                .Map(dest => dest.IdentityDocuments, src => MapContext.Current.Parameters["IdentitiDocuments"])
                .Map(dest => dest.Name, src => src.FirstName + " " + (src.SecondName ?? string.Empty))
                .Map(dest => dest.Surname, src => src.Surname)
                .Map(dest => dest.NationalIdentificationNumber, src => src.NationalIdentificationNumber);
        }
    }
}
