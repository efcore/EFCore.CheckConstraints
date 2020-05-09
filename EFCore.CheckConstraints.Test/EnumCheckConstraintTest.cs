using System;
using System.Linq;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFCore.CheckConstraints.Test
{
    public class EnumCheckConstraintConventionTest
    {
        [Fact]
        public void Generate_check_constraint_with_all_enum_names()
        {
            var builder = CreateBuilder();
            builder.Entity<Order>()
                .Property(o => o.OrderStatus).HasConversion<string>();

            var model = builder.FinalizeModel();

            var checkConstraint = model.FindEntityType(typeof(Order))
                .GetCheckConstraints()
                .FirstOrDefault(constraint => constraint.Name == "CK_Order_OrderStatus_Enum_Constraint");

            Assert.NotNull(checkConstraint);
            Assert.Equal("CK_Order_OrderStatus_Enum_Constraint", checkConstraint.Name);
            Assert.Equal("[OrderStatus] IN (N'Active', N'Completed')", checkConstraint.Sql);
        }

        [Fact]
        public void Generate_check_constraint_with_all_enum_values()
        {
            var builder = CreateBuilder();
            builder.Entity<Customer>();

            var model = builder.FinalizeModel();

            var checkConstraint = model.FindEntityType(typeof(Customer))
                .GetCheckConstraints()
                .FirstOrDefault(constraint => constraint.Name == "CK_Customer_CustomerType_Enum_Constraint");

            Assert.NotNull(checkConstraint);
            Assert.Equal("CK_Customer_CustomerType_Enum_Constraint", checkConstraint.Name);
            Assert.Equal("[CustomerType] IN (0, 1)", checkConstraint.Sql);
        }

        [Fact]
        public void Should_not_generate_gheck_constraint_for_empty_enum()
        {
            var builder = CreateBuilder();
            builder.Entity<Seller>()
                .Property(p => p.SellerStatusString)
                .HasConversion<string>();

            var model = builder.FinalizeModel();

            var checkConstraintString = model.FindEntityType(typeof(Seller))
                .GetCheckConstraints()
                .FirstOrDefault(constraint => constraint.Name == "CK_Seller_SellerStatusString_Enum_Constraint");

            var checkConstraintInt = model.FindEntityType(typeof(Seller))
                .GetCheckConstraints()
                .FirstOrDefault(constraint => constraint.Name == "CK_Seller_checkConstraintInt_Enum_Constraint");

            Assert.Null(checkConstraintString);
            Assert.Null(checkConstraintInt);
        }

        [Fact]
        public void Should_not_generate_check_constraint_for_enum_with_flag()
        {
            var builder = CreateBuilder();
            builder.Entity<File>();

            var model = builder.FinalizeModel();

            var checkConstraint = model.FindEntityType(typeof(File))
                .GetCheckConstraints()
                .FirstOrDefault(constraint =>
                    constraint.Name == "CK_File_FileStatus_Enum_Constraint");

            Assert.Null(checkConstraint);
        }

        private class Seller
        {
            public int Id { get; set; }
            public string SellerName { get; set; }
            public SellerStatus SellerStatusString { get; set; }
            public SellerStatus SellerStatusInt { get; set; }
        }

        private enum SellerStatus
        {
        }

        private class Order
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public OrderStatus OrderStatus { get; set; }
        }

        private enum OrderStatus
        {
            Active,
            Completed
        }

        private class Customer
        {
            public int Id { get; set; }
            public int CustomerName { get; set; }
            public CustomerType CustomerType { get; set; }
        }

        private enum CustomerType
        {
            Standard = 0,
            Premium = 1
        }

        private class File
        {
            public int Id { get; set; }
            public string Path { get; set; }
            public FileStatus FileStatus { get; set; }
        }

        [Flags]
        private enum FileStatus
        {
            Opened = 0x0,
            Closed = 0x1,
            ReadOnly = 0x2,
            WriteOnly = 0x4
        }

        private ModelBuilder CreateBuilder()
        {
            var conventionSet = SqlServerTestHelpers.Instance.CreateContextServices()
                .GetRequiredService<IConventionSetBuilder>()
                .CreateConventionSet();

            conventionSet.ModelFinalizingConventions.Add(new EnumCheckConstraintConvention());

            return new ModelBuilder(conventionSet);
        }
    }
}
