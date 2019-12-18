# Moonglade

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade-Master-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=50)

The blog system for https://edi.wang. Written in C# on [**.NET Core**](https://dotnet.microsoft.com/) and runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/).

![image](https://cdn-blob.edi.wang/web-assets/ediwang-azure-arch-v2.png)

## Features

**Basic:** Post, Comment, Category, Archive, Tag, Page, Friendlink

**Misc:** Pingback, RSS/Atom/OPML, Open Search, Reader View

## Caveats

This is **NOT a general purpose blog system** like WordPress or other CMS. Currently it contains content "hard coded" for https://edi.wang. To make it yours, you will need to change a certain amount of code.

## Build and Run

> The following tools are required for development.

Tools | Alternative
--- | ---
[.NET Core 3.1 SDK](http://dot.net) | N/A
[Visual Studio 2019](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/)
[Azure SQL Database](https://azure.microsoft.com/en-us/services/sql-database/) | [SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-2019) / LocalDB (Dev Only)

### Setup Database

#### 1. Create Database 

##### Development

Create an [SQL Server 2019 LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?WT.mc_id=AZ-MVP-5002809&view=sql-server-ver15) database. e.g. **moonglade-dev**

##### Production

[Create an Azure SQL Database](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-database-get-started?WT.mc_id=AZ-MVP-5002809) or a SQL Server 2019 database. e.g. **moonglade-production**

#### 2. Set Connection String

##### Via configuration file

Update the connection string "**MoongladeDatabase**" in **appsettings.[env].json** according to your database configuration.

Example:
```json
"ConnectionStrings": {
  "MoongladeDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=moonglade-dev;Trusted_Connection=True;"
}
```

##### Via environment variable (Recommend for production)

Set environment variable: ```ConnectionStrings__MoongladeDatabase``` to your connection string. If you are deploying to Azure App Service, you can set the connection string in the Configuration blade.

### Build Source

1. Create an "**appsettings.Development.json**" under "**src\Moonglade.Web**", this file defines development time settings such as accounts, db connections, keys, etc. It is by default ignored by git, so you will need to manange it on your own.

2. Build and run **Moonglade.sln**

## Configuration

> Below section discuss system settings in **appsettings.[env].json**. For blog settings, please use "/admin/settings" UI.

### Authentication

Configure how to sign in to admin portal.

#### Preferred: [Azure Active Directory]((https://azure.microsoft.com/en-us/services/active-directory/))

Register an App in **Azure Active Directory**
- Set Redirection URI to **"https://yourdomain/signin-oidc"**
  - For local debugging, set URL to https://localhost:5001/signin-oidc
- Copy "**appId**" to set as **AzureAd:ClientId** in **appsettings.[env].json** file

```json
"Authentication": {
  "Provider": "AzureAD",
  "AzureAd": {
    "Domain": "{YOUR-VALUE}",
    "TenantId": "{YOUR-VALUE}",
    "ClientId": "{YOUR-VALUE}",
  }
}
```

#### Alternative: Local Account

Set **Authentication:Provider** to **"Local"** and assign a pair of username and password. 

*Currently password is not encrypted, use it at your own risk.*

```json
"Authentication": {
  "Provider": "Local",
  "Local": {
    "Username": "{YOUR-VALUE}",
    "Password": "{YOUR-VALUE}",
  }
}
```

### Image Storage
**AppSettings:ImageStorage** controls how blog post images are stored. There are 2 built-in options:

#### [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) (Preferred)

You need to create an [**Azure Blob Storage**](https://azure.microsoft.com/en-us/services/storage/blobs/) with **container level permission**. 

```json
"Provider": "azurestorage"
"AzureStorageSettings": {
  "ConnectionString": "YOUR CONNECTION STRING",
  "ContainerName": "YOUR CONTAINER NAME"
}
```

#### File System (Alternative)

```json
"Provider": "filesystem",
"FileSystemSettings": {
  "Path": "${basedir}\\UploadedImages"
}
```
The **Path** can be relative or absolute. **"$\{basedir\}"** represents the website's current directory. Storing images files under website directory is NOT recommended. 

#### CDN

If **GetImageByCDNRedirect** is set to **true**, the blog will get images from client browser using a 302 redirect, not by fetching images in backend and put into memory cache. This is especially useful when you have a CDN for your image resources, like what I did on Azure. 

```json
"CDNSettings": {
    "GetImageByCDNRedirect": true,
    "CDNEndpoint": "https://ews.azureedge.net/ediwang-images"
}
```

### Email Notification

If you need email notification for new comments, new replies and pingbacks, you have to setup the Moonglade.Notification API first. 

#### Setup Moonglade.Notification API

See https://github.com/EdiWang/Moonglade.Notification for instructions

#### Configure Moonglade

Set values in AppSettings:

```json
"Notification": {
  "Enabled": true,
  "ApiEndpoint": "{PROD-ENV-VARIABLE}",
  "ApiKey": "{PROD-ENV-VARIABLE}"
}
```

### Robots.txt

This blog generates robots.txt based on configuration. However, if there are a physical file named "robots.txt" under "wwwroot" directory, it will override the configuration based robots.txt generation.

To customize robots.txt, modify the configuration under **RobotsTxt** section.

### Others

Key | Description
--- | ---
Editor | HTML / Markdown
CaptchaSettings:ImageWidth | Pixel Width of Captcha Image
CaptchaSettings.ImageHeight | Pixel Height of Captcha Image
PostSummaryWords | How may words to show in post list summary
ImageCacheSlidingExpirationMinutes | Time for cached images to expire
EnforceHttps | Force website use HTTPS
AllowScriptsInCustomPage | Allow JavaScript in Page content or not
EnableImageHotLinkProtection | Prevent images from being hot link from other sites*

> Due to platform limitation, image hot link prevention requires manually edit file ```src\Moonglade.Web\urlrewrite.xml``` before deployment. 

## FAQ

### Does this blog support upgrade from a lower version?

It depends. If the database schema is same for a higer version, then the system can be deployed and override old files without problem.

### Does this blog coupled with Microsoft Azure?

No, the system design does not couple with Azure, but the blog works best on Azure. Every part of the system, like Authentication and Image Storage, can be configured to use non-Azure options.

## Related Projects

> Below open source projects are reusable components (NuGet packages) used in my blog, and they can be used in other websites as well. 

Repository | Nuget
--- | ---
[Edi.Blog.Pingback](https://github.com/EdiWang/Edi.Blog.Pingback) | [![NuGet][main-nuget-badge-1]][main-nuget-1]
[Edi.Blog.OpmlFileWriter](https://github.com/EdiWang/Edi.Blog.OpmlFileWriter) | [![NuGet][main-nuget-badge-2]][main-nuget-2]
[Edi.Captcha.AspNetCore](https://github.com/EdiWang/Edi.Captcha.AspNetCore) | [![NuGet][main-nuget-badge-3]][main-nuget-3]
[Edi.ImageWatermark](https://github.com/EdiWang/Edi.ImageWatermark) | [![NuGet][main-nuget-badge-4]][main-nuget-4]
[Edi.Net.AesEncryption](https://github.com/EdiWang/Edi.Net.AesEncryption) | [![NuGet][main-nuget-badge-5]][main-nuget-5]
[Edi.Practice.RequestResponseModel](https://github.com/EdiWang/Edi.Practice.RequestResponseModel) | [![NuGet][main-nuget-badge-6]][main-nuget-6]
[Edi.SyndicationFeedGenerator](https://github.com/EdiWang/Edi.SyndicationFeedGenerator) | [![NuGet][main-nuget-badge-7]][main-nuget-7]
[Edi.TemplateEmail](https://github.com/EdiWang/Edi.TemplateEmail) | [![NuGet][main-nuget-badge-8]][main-nuget-8]
[Edi.WordFilter](https://github.com/EdiWang/Edi.WordFilter) | [![NuGet][main-nuget-badge-9]][main-nuget-9]

[main-nuget-1]: https://www.nuget.org/packages/Edi.Blog.Pingback/
[main-nuget-badge-1]: https://img.shields.io/nuget/v/Edi.Blog.Pingback.svg?style=flat-square&label=nuget

[main-nuget-2]: https://www.nuget.org/packages/Edi.Blog.OpmlFileWriter/
[main-nuget-badge-2]: https://img.shields.io/nuget/v/Edi.Blog.OpmlFileWriter.svg?style=flat-square&label=nuget

[main-nuget-3]: https://www.nuget.org/packages/Edi.Captcha/
[main-nuget-badge-3]: https://img.shields.io/nuget/v/Edi.Captcha.svg?style=flat-square&label=nuget

[main-nuget-4]: https://www.nuget.org/packages/Edi.ImageWatermark/
[main-nuget-badge-4]: https://img.shields.io/nuget/v/Edi.ImageWatermark.svg?style=flat-square&label=nuget

[main-nuget-5]: https://www.nuget.org/packages/Edi.Net.AesEncryption/
[main-nuget-badge-5]: https://img.shields.io/nuget/v/Edi.Net.AesEncryption.svg?style=flat-square&label=nuget

[main-nuget-6]: https://www.nuget.org/packages/Edi.Practice.RequestResponseModel/
[main-nuget-badge-6]: https://img.shields.io/nuget/v/Edi.Practice.RequestResponseModel.svg?style=flat-square&label=nuget

[main-nuget-7]: https://www.nuget.org/packages/Edi.SyndicationFeedGenerator/
[main-nuget-badge-7]: https://img.shields.io/nuget/v/Edi.SyndicationFeedGenerator.svg?style=flat-square&label=nuget

[main-nuget-8]: https://www.nuget.org/packages/Edi.TemplateEmail/
[main-nuget-badge-8]: https://img.shields.io/nuget/v/Edi.TemplateEmail.svg?style=flat-square&label=nuget

[main-nuget-9]: https://www.nuget.org/packages/Edi.WordFilter/
[main-nuget-badge-9]: https://img.shields.io/nuget/v/Edi.WordFilter.svg?style=flat-square&label=nuget
