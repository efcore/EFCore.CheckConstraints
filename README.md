# Check constraints for Entity Framework Core

[![Nuget](https://img.shields.io/nuget/v/EFCore.CheckConstraints)](https://www.nuget.org/packages/EFCore.CheckConstraints)

Many databases support something called "check constraints", which allow you to define arbitrary validation for the rows of a table. Think about it like a boring column unique constraint, but on steroids - you can specify that every customer in your table must be either over 18, or have the "parents' permission" bit on. Or whatever makes sense. Just run with it.

Entity Framework Core allows you to specify check constraints in SQL - this helps tighten your data model and ensure that no inconsistent or invalid ever makes it into your precious tables. However, EF does not implicitly generate check constraints for you, even though in some cases it could; this is because check constraints do have a performance cost, and they're not for everyone. This plugin allows you to opt into some constraints - just activate it and they'll automatically get created for you.

The first step is to install the [EFCore.CheckConstraints nuget package](https://www.nuget.org/packages/EFCore.CheckConstraints). Then, choose the constraints you want from the below.

## Enum constraints

When you map a .NET enum to the database, by default that's done by storing the enum's underlying int in a plain old database int column (another common strategy is to map the string representation instead). Although the .NET enum has a constrained set of values which you've defined, on the database side there's nothing stopping anyone from inserting any value, including ones that are out of range.

Activate enum check constraints as follows:

```c#
public class Order
{
    public int Id { get; set; }
    public OrderStatus OrderStatus { get; set; }
}

public enum OrderStatus
{
    Active,
    Completed
}

public class MyContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder
            .UseSqlServer(...)
            .UseEnumCheckConstraints();
}
```

This will cause the following table to be created:

```sql
CREATE TABLE [Order] (
    [Id] int NOT NULL IDENTITY,
    [OrderStatus] int NOT NULL,
    CONSTRAINT [PK_Order] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Order_OrderStatus_Enum_Constraint] CHECK ([OrderStatus] IN (0, 1))
);
```

The added CHECK constraint allows only 0 and 1 to be stored in the column, ensuring better data integrity.

## Discriminator constraints

EF Core allows you to map a .NET type hierarchy to a single database table; this pattern is called Table-Per-Hierarchy, or TPH. When using this mapping pattern, a *discriminator* column is added to your table, which determines which entity type is represented by the particular row; when reading query results from the database, EF will materialize different .NET types in the hierarchy based on this value. You can read more about TPH and discriminators in the [EF docs](https://docs.microsoft.com/ef/core/modeling/inheritance).

In the typical case, your hierarchy will have a closed set of .NET types; but as with enums, the database discriminator column can contain anything. If EF encounters an unknown discriminator value when reading query results, the query will fail. You can instruct the plugin to create check constraints to make sure this doesn't happen:

```c#
public class Parent
{
    // ...
}

public class Sibling1 : Parent
{
    // ...
}

public class Sibling2 : Parent
{
    // ...
}
```

This will cause the following table to be created:

```sql
CREATE TABLE [Parent] (
    [Id] int NOT NULL IDENTITY,
    [Discriminator] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Parent] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Parent_Discriminator_Constraint] CHECK ([Discriminator] IN (N'Parent', N'Sibling1', N'Sibling2'))
);
```

## I want them all!

In love with check constraints? Simply specify `UseAllCheckConstraints` to set everything up.

## Important note

This is a community-maintained plugin: it isn't an official part of Entity Framework Core and isn't supported by Microsoft in any way.
