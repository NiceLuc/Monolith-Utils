# Delinq CLI - Linq Conversion Tool

This app will parse a LINQ designer file and extract the necessary data that it needs to recreate the queries to a backing "Repository" approach. This will allow the queries to be run against a database without the need for the LINQ designer file.

## Features

This is a command line tool that is intended to generate properly formatted C# code for any Repository with valid unit tests which ensure the methods are protected from future changes.

### Parse DataContext File

Before we can generate code, we must first define what methods and parameters are used in the existing implementation.

```powershell
$> delinq init "c:\temp\SomeDataContext.dbml"
```

This will create a `SomeData.metadata.settings` file which contains all the information needed to generate repository methods and unit tests.

### Generate Repository Files

We defined a standard approach for calling the database.
* `DataModels.cs` - Contains all of the `DTO` objects in one code file.
* `DataContext.cs` - A new (very light-weight) implementation of the original DataContext.
* `IRepository.cs` - An interface of all required methods that match the original DataContext methods.
* `Repository.cs` - The customized implementation of all stored procedure calls.
* `IRepositorySettings.cs` - The interface that is given to the repository for configurations needed to connect to the database.
* `RepositorySettings.cs` - The implementation of reading the connection settings from a config file.

Here is the line of code that will generate all of the above files:

```powershell
$> delinq repo "c:\temp\SomeData.metadata.settings" -o "c:\temp\SomeData\RepoFiles"
```

If you only need to generate code for a specific method, you can define it using a flag:

```powershell
$> delinq repo "c:\temp\SomeData.metadata.settings" -o "c:\temp\SomeData\RepoFiles" -m SomeSpecificMethodName
```

This is useful if you only have to generate code for a specific database call.

### Generate Unit Test Files

We defined a standard approach for testing our repository methods.
* All mock setups are **STRICT** to ensure our repository code works as expected (always).
* Code should be properly formatted (tabs, vertical line spacing, usage of `var`, etc...)
* Every repository method should have a **Happy Path** test and an **Throws Exception** test.
* Every unit test should use predefined extension methods to make tests small and consumable.
  * Extension methods were created for `NonQuery`, `QueryMany` and `QuerySingle` database calls.

Here is the line of code that will generate all of the above files:

```powershell
$> delinq tests "c:\temp\SomeData.metadata.settings" -o "c:\temp\SomeData\TestFiles"
```

If you only need to generate test code for a specific repository method, you can define it using a flag:

```powershell
$> delinq tests "c:\temp\SomeData.metadata.settings" -o "c:\temp\SomeData\TestFiles" -m SomeSpecificMethodName
```

This is useful if you only have to generate code for a specific database call.

## For More Information

Feel free to use the custom calls defined in the `Delinq/Properties/launchSettings.json` file to get started.
