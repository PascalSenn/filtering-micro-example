using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Filtering.Demo
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGraphQLServer()
                .AddQueryType<Query>()
                .AddFiltering<CustomFilterConvention>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(
                endpoints => { endpoints.MapGraphQL(); });
        }
    }

    public class Query
    {
        [UseFiltering]
        public IQueryable<User> GetUsers() => DBContext.Users;
    }

    public class User
    {
        public string Name { get; set; }

        public int DirectCommitsToMaster { get; set; }

        public List<Address> Addresses { get; set; }

        public Company Company { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
    }

    public class Company
    {
        public string Name { get; set; }

        public static Company Default = new Company { Name = "ChilliCream" };
    }

    public class CustomFilterConvention : FilterConvention
    {
        protected override void Configure(IFilterConventionDescriptor descriptor)
        {
            descriptor.Operation(123).Name("example").Description("This is a example");
            descriptor.Operation(DefaultOperations.Contains)
                .Name("_contains")
                .Description("This is a example");

            descriptor.ArgumentName("custom");

            descriptor.BindRuntimeType<Address, AddressFilterInputType>();

            descriptor.Configure<StringOperationFilterInput>(
                x => x.Field("test").Type<StringType>());

            descriptor.AddDefaults();

            descriptor.Provider<CustomProvider>();
        }
    }

    public class AddressFilterInputType : FilterInputType<Address>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Address> descriptor)
        {
            descriptor.Operation(123).Type<CustomOperationInputType>();
        }
    }

    public class CustomOperationInputType : ComparableOperationFilterInput<int>
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Field("data").Type<IntType>();
        }
    }

    public class CustomStringScalar : StringOperationFilterInput
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Field("example").Type<StringType>();
        }
    }

    public class CustomProvider : QueryableFilterProvider
    {
        protected override void Configure(
            IFilterProviderDescriptor<QueryableFilterContext> descriptor)
        {
            descriptor.AddDefaultFieldHandlers();
            descriptor.AddFieldHandler<CustomFieldHandler>();
            descriptor.AddFieldHandler<MatchAllFields>();
            base.Configure(descriptor);
        }
    }

    public class CustomFieldHandler : QueryableOperationHandlerBase
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition definition &&
                definition.Id == 123;
        }

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MatchAllFields : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return true;
        }
    }

    public class DBContext
    {
        public static IQueryable<User> Users = new User[]
        {
            new User
            {
                Name = "Pascal",
                DirectCommitsToMaster = 0,
                Addresses = new List<Address>
                {
                    new Address
                    {
                        Street = "A street",
                    },
                    new Address
                    {
                        Street = "Another street",
                    }
                },
                Company = Company.Default
            },
            new User
            {
                Name = "Michael",
                DirectCommitsToMaster = 284,
                Addresses = new List<Address>
                {
                    new Address
                    {
                        Street = "Some Street",
                    },
                },
                Company = Company.Default
            },
            new User
            {
                Name = "Raphael",
                DirectCommitsToMaster = 0,
                Addresses = new List<Address>
                {
                    new Address
                    {
                        Street = "Sample Road",
                    },
                },
                Company = Company.Default
            },
        }.AsQueryable();
    }
}