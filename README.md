# Project "Moonglade"

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade-Master-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=50)

The new blog system for https://edi.wang. Written in C# on [**.NET Core**](https://dotnet.microsoft.com/) and runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/).

![image](https://ews.azureedge.net/web-assets/ediwang-azure-arch-v2.png)

## Features

**Basic:** Post, Comment, Category, Archive, Tag, Page, Friendlink

**Misc:** Pingback, RSS/Atom/OPML, Open Search, Reader View

## Caveats

This is **NOT a general purpose blog system** like WordPress or other CMS. Currently it contains content "hard coded" for https://edi.wang. To make it yours, you will need to change a certain amount of code.

## Build and Run

> The following tools are required for development.

Tools | Alternative
--- | ---
[.NET Core 2.2 SDK](http://dot.net) | N/A
[Visual Studio 2019](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/)
[Azure SQL Database](https://azure.microsoft.com/en-us/services/sql-database/) | [SQL Server 2017](https://www.microsoft.com/en-us/sql-server/sql-server-2017) / LocalDB (Dev Only)

### Setup Database

#### 1. Create Database 

##### For Development (Light Weight, Recommended for Windows)

Create an [SQL Server 2017 LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-2017) database. e.g. moonglade-dev

##### For Production

[Create an Azure SQL Database](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-database-get-started) or a SQL Server 2017+ database. e.g. moonglade-dev

#### 2. Set Connection String

Update the connection string "**MoongladeDatabase**" in **appsettings.[env].json** according to your database configuration.

Example:
```json
"ConnectionStrings": {
  "MoongladeDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=moonglade-dev;Trusted_Connection=True;"
}
```

*The blog will automatically setup datbase schema and initial data in first run*

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
  "Provider": "aad",
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
  "Provider": "local",
  "Local": {
    "Username": "{YOUR-VALUE}",
    "Password": "{YOUR-VALUE}",
  }
}
```

### Image Storage
**AppSettings:ImageStorage** controls how blog post images are stored. There are 2 built-in options:

#### Preferred: [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/)

You need to create an [**Azure Blob Storage**](https://azure.microsoft.com/en-us/services/storage/blobs/) with **container level permission**. 

```json
"Provider": "azurestorage"
"AzureStorageSettings": {
  "ConnectionString": "YOUR CONNECTION STRING",
  "ContainerName": "YOUR CONTAINER NAME"
}
```

#### Alternative: File System

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

### Email Password Encryption

**Encryption** controls the **IV** and **Key** for encrypted email passwords in database. 

*The blog will try to generate a pair of Key and IV on first run, and write values into appsettings.**[Current Environment]**.json only. This means the application directory **must NOT be read only**. You'll have to set keys manully if you must use a read only deployment.*

### Robots.txt

This blog generates robots.txt based on configuration. However, if there are a physical file named "robots.txt" under "wwwroot" directory, it will override the configuration based robots.txt generation.

To customize robots.txt, modify the configuration under **RobotsTxt** section.

### Others

Key | Description
--- | ---
CaptchaSettings:ImageWidth | Pixel Width of Captcha Image
CaptchaSettings.ImageHeight | Pixel Height of Captcha Image
TimeZone | The blog owner's current time zone (relative to UTC)
PostSummaryWords | How may words to show in post list summary
ImageCacheSlidingExpirationMinutes | Time for cached images to expire
EnableImageLazyLoad | Use lazy load to show images when user scrolls the page
EnablePingBackReceive | Can blog receive pingback requests
EnablePingBackSend | Can blog send pingback to another blog
EnforceHttps | Force website use HTTPS
DisableEmailSendingInDevelopment | When debugging locally, do not send email for real
DNSPrefetchEndpoint | Add HTML head named "dns-prefetch"

### URL Rewrite

The only built-in rule is removing trailing slash in URLs. For other rules, you can customize by editing "\src\Moonglade.Web\urlrewrite.xml" according to [IIS URL Rewrite Module configuration](https://www.iis.net/downloads/microsoft/url-rewrite)

## FAQ

### Does this blog support upgrade from a lower version?

Not yet. Currently it depends on whether the database schema is changed. If the schema is same for a higer version, then the system can be deployed and override old files without problem.

### Does this blog coupled with Microsoft Azure?

No, the system design does not couple with Azure, but the blog works best on Azure. Every part of the system, like Authentication and Image Storage, can be configured to use non-Azure options.

## Optional Recommendations
- [Microsoft Azure DNS Zones](https://azure.microsoft.com/en-us/services/dns/)
- [Microsoft Azure App Service](https://azure.microsoft.com/en-us/services/app-service/)
- [Microsoft Azure SQL Database](https://azure.microsoft.com/en-us/services/sql-database/)
- [Microsoft Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/)

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

[main-nuget-8]: https://www.nuget.org/packages/Edi.TemplateEmail.NetStd/
[main-nuget-badge-8]: https://img.shields.io/nuget/v/Edi.TemplateEmail.NetStd.svg?style=flat-square&label=nuget

[main-nuget-9]: https://www.nuget.org/packages/Edi.WordFilter/
[main-nuget-badge-9]: https://img.shields.io/nuget/v/Edi.WordFilter.svg?style=flat-square&label=nuget