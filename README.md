# What is Monolith-Utils?

Monolith-Utils is a collection of command line tools to help migrate monolithic applications to a more modern architecture.

# Monolith Utils Setup

## Download latest Monolith-Utils github repository.

* Github Desktop download: [Download GitHub Desktop](https://desktop.github.com/download/)
* Capture HTTPS link and download the repository using Github Desktop
* Compile the project and make sure it builds (✅).
* Ensure the **MonoUtils.CLI/appsettings.json** file points to correct paths!
	* After changes, be sure to compile again!

## Setup Command Line Environment:

Our tool is **100% command line**, and **VERY powerful**! 

Before continuing, it's best to have a command-line accessible text editor.
If you don't already have one, consider using **Visual Studio Code**
* Download latest [Visual Studio Code](https://code.visualstudio.com/download)
	
## Open Command Line

Open up a **_Linux_**-based command line tool (Github has one or you can use WSL).
 
```shell
touch ~/.bashrc && code ~/.bashrc
```

In your `.bashrc` file, add the following lines to the bottom of your file:
* If you are using WSL, be sure to put `/mnt` before all file paths.

```shell
alias muthu="/c/…/MonoUtils.App/bin/debug/net8.0/muthu.exe"
alias delinq="/c/…/MonoUtils.Delinq/bin/debug/net8.0/delinq.exe"
```

**Save and close.**

Back at your command line, type the following:

```shell	
source ~/.bashrc

muthu --help
delinq --help
```

Should see output.

# Using Monolith Utils

There are 2 main libraries that are versioned in this repository:
* `muthu` - Used to understand all solutions and projects in a monolith directory structure.
* `delinq` - Used to convert LINQ queries to Repository pattern.

-----

# Muthu CLI - Monolith Utilities To Help Unravel

At this time, there is not a lot of information, as it's evolving very fast. 

> **IMPORTANT** - You must first ensure that the `appsettings.json` file in the `MonoUtils.CLI` directory points to the correct paths for your environment.

For now, simply use the following:

```shell
muthu --help            # use this to see all commands
muthu branch --help     # manage which TFS branch you are working on
muthu init --help		# REQUIRED! initialize the database for analysis
muthu projects --help	# find information about all projects in the monolith
muthu project --help	# get detailed information about a project in the monolith
muthu solutions --help	# find informationa bout all solutions in the monolith
muthu solution --help	# get detailed information about a solution in the monolith
muthu wix --help		# analyze a specific WiX project file
```

-----

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

### Validate Repository Files

This is used to ensure that we have accounted for proper implementations of the repository methods.
* Parse the repository files and ensure that all methods are accounted for.
* Capture all parameters from the method definition.
* Look for specific calls inside the code to ensure that the parameters are being used correctly.
* Connect to the database and read the stored procedure that is being called from the method.
* Capture all parameters from the stored procedure definition.
* Look for specific calls inside the stored procedure to determine what the "QueryType" should be
* Compare the repository implementation to the stored procedure and set the Status for each method.

```powershell
$> delinq verify "c:\temp\SomeRepository.cs" SECRET:ConnectionStrings.InCode -o "c:\temp\Verification.json"
```

> **Note:** This requires user secrets to be set up for the connection string.

```shell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:InCode" "YourConnectionString"
```

### Generate Verification Report

Reads the json file generated from the `verify` results and generates a report of the findings.

> **NOTE**: The results are output to "csv" format, but can be opened in Excel to filter and sort results easily.

```powershell
$> delinq report "c:\temp\Verification.json"
```

