using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Linq;

using Rock.Data;
using Rock.Model;
using Rock.Tests.Data;
using Xunit;


namespace Rock.Tests.Rock
{
    public class MoqTest : IClassFixture<DatabaseTests>
    {
        [Fact]
        public void MyTest()
        {
            using ( var rockContext = new RockContext() )
            {
                var adults = new PersonService( rockContext ).GetAllAdults();
                var adultCount = adults.Where( p => p.NickName == "Giver" ).Count();

                Assert.Equal( 1, adultCount );
            }
        }

        [Fact]
        public void Test1()
        {
            using ( var rockContext = new RockContext() )
            {
                var adults = new PersonService( rockContext ).GetAllAdults();
                var adultCount = adults.Where( p => p.FirstName == "Ted" ).Count();

                var entityTypeService = new EntityTypeService( rockContext );
                var count = entityTypeService.Queryable().Count();

                var categoryService = new CategoryService( rockContext );
                var list = categoryService.Queryable().ToList();
                var et = list.First().EntityType;
                var category = new Category { Id = 2, EntityTypeId = 1, Name = "Root" };
                categoryService.Add( category );
                list = categoryService.Queryable().ToList();
            }
        }

        [Fact]
        public void Test2()
        {
            using ( var rockContext = new RockContext() )
            {
                var adults = new PersonService( rockContext ).GetAllAdults();
                var adultCount = adults.Where( p => p.FirstName == "Ted" ).Count();
            }
        }
    }

    public class MoqTest2 : DatabaseTests
    {
        [Fact]
        public void Test3()
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.CreateIfNotExists();

                var adults = new PersonService( rockContext ).GetAllAdults();
                var adultCount = adults.Where( p => p.FirstName == "Ted" ).Count();
            }
        }

        [Fact]
        public void Test4()
        {
            using ( var rockContext = new RockContext() )
            {
                var adults = new PersonService( rockContext ).GetAllAdults();
                var adultCount = adults.Where( p => p.FirstName == "Ted" ).Count();
            }
        }

        [Fact]
        public void Test5()
        {
            using ( var rockContext = new RockContext() )
            {
                var adults = new PersonService( rockContext ).GetAllAdults();
                var adultCount = adults.Where( p => p.NickName == "Giver" ).Count();

                Assert.Equal( 1, adultCount );
            }
        }

        [Fact]
        public void Test6()
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.CreateIfNotExists();

                var adults = new PersonService( rockContext ).GetAllAdults();
                var adultCount = adults.Where( p => p.FirstName == "Ted" ).Count();
            }
        }
    }
}
