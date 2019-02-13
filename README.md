# Project "Moonglade"

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade-Master-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=50)

This is the new blog system for https://edi.wang, Moonglade project is the successor of project "Nordrassil", which was the .NET Framework version of the blog system. Moonglade is a complete rewrite of the old system using .NET Core, focus on performance and optimized for cloud-based hosting.

## Blog Features
- Post
- Comment
- Category
- Tag
- Pingback
- RSS/Atom/OPML
- Open Search

## Major Technologies and Frameworks
- ASP.NET Core
- Entity Framework Core
- SQL Server
- Bootstrap 4
- Fontawesome 5
- TinyMCE

## Build and Run

### Tools
- .NET Core 2.2 SDK
- Visual Studio 2017 or Visual Studio Code
- SQL Server 2017+ / Azure SQL Database Instance

### Steps

#### Prepare Azure Environment

- Register an App in Azure Active Directory
- Create an Azure Storage Account (optional but enabled by default, for saving blog post images)

#### Build Source

1. You will need to create a "**appsettings.Development.json**" under ".\src\Moonglade.Web", this file defines development time settings such as accounts, db connections, keys, etc. It is by default ignored by git, so you will need to manange it on your own.

2. Create a SQL Server dabase using "**.\Database\schema-mssql-140.sql**", execute "**.\Database\Migration.sql**", and update the connection string 'MoongladeDatabase' in **appsettings.Development.json**. 

3. Build **Moonglade.sln**, startup project is: ".\src\Moonglade.Web\Moonglade.Web.csproj"

## Host on Production

Windows or Linux Servers that supports .NET Core 2.2

### Required
- Microsoft Azure Active Directory

### Optional
- Microsoft Azure App Service
- Microsoft Azure SQL Database
- Microsoft Azure Blob Storage