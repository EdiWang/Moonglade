# Project "Moonglade"

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade-Master-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=50)

Moonglade is the new blog system for https://edi.wang. It is a complete rewrite of the old system using [**.NET Core**](https://dotnet.microsoft.com/) and runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/).

![image](https://ediwangstorage.blob.core.windows.net/web-assets/ediwang-azure-arch.png?date=20190413)

## Features

**Basic:** Post, Comment, Category, Archive, Tag, Friendlink

**Misc:** Pingback, RSS/Atom/OPML, Open Search, Reader View

## Caveats

This is **NOT a general purpose blog system** like WordPress or other CMS. Currently it contains content "hard coded" for https://edi.wang. To make it yours, you will need to change a certain amount of code.

> I am generalizing the system piece by piece. But there are no specific plans and scopes currently.

## Build and Run

### Tools and Dependencies
- [.NET Core 2.2 SDK](http://dot.net)
- [Visual Studio 2019](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Azure SQL Database](https://azure.microsoft.com/en-us/services/sql-database/) or [SQL Server 2017](https://www.microsoft.com/en-us/sql-server/sql-server-2017)
- [Microsoft Azure Subscription](https://azure.microsoft.com/)

### Setup Database

1. [Create an Azure SQL Database](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-database-get-started) or a SQL Server 2017+ database and run **"Database\schema-mssql-140.sql"** 

2. Update the connection string "**MoongladeDatabase**" in **appsettings.[env].json** according to your database configuration.

Example:
```json
"ConnectionStrings": {
  "MoongladeDatabase": "Server=(local);Database=moonglade-dev;Trusted_Connection=True;"
}
```

### Build Source

1. Create an "**appsettings.Development.json**" under "**src\Moonglade.Web**", this file defines development time settings such as accounts, db connections, keys, etc. It is by default ignored by git, so you will need to manange it on your own.

2. Build and run **Moonglade.sln**

### Configuration

> Below section discuss system settings in **appsettings.[env].json**. For blog settings, please use "/admin/settings" UI.

#### Authentication

Configure how to sign in to admin portal.

##### Preferred: [Azure Active Directory]((https://azure.microsoft.com/en-us/services/active-directory/))

This is the most wonderful SSO solution!

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

##### Alternative: Local Account

> Currently under construction. Local authentication provider is arriving soon. 

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

#### Image Storage
**AppSettings:ImageStorage** controls how blog post images are stored. There are 2 built-in options:

**Preferred: [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/)**

You need to create an [**Azure Blob Storage**](https://azure.microsoft.com/en-us/services/storage/blobs/) with **container level permission**. 
```json
"Provider": "azurestorage"
"AzureStorageSettings": {
  "ConnectionString": "YOUR CONNECTION STRING",
  "ContainerName": "YOUR CONTAINER NAME"
},
```

**Alternative: File System**

```json
"Provider": "filesystem",
"FileSystemSettings": {
  "Path": "${basedir}\\UploadedImages"
}
```
The **Path** can be relative or absolute. **"$\{basedir\}"** represents the website's current directory. Storing images files under website directory is NOT recommended. 

#### Email Password Encryption

**Encryption** controls the **IV** and **Key** for encrypted email passwords in database. 

*The blog will try to generate a pair of Key and IV on first run, and write values into appsettings.**[Current Environment]**.json only. This means the application directory **must NOT be read only**. You'll have to set keys manully if you must use a read only deployment.*

To get a random generated key, access URL "/admin/settings/generate-new-aes-keys".

#### Robots.txt

This blog generates robots.txt based on configuration. However, if there are a physical file named "robots.txt" under "wwwroot" directory, it will override the configuration based robots.txt generation.

To customize robots.txt, modify the configuration under **RobotsTxt** section.

#### Others

Key | Description
--- | ---
CaptchaSettings:ImageWidth | Pixel Width of Captcha Image
CaptchaSettings.ImageHeight | Pixel Height of Captcha Image
TimeZone | The blog owner's current time zone (relative to UTC)
HotTagAmount | How many tags to show on the side bar
PostListPageSize | How may posts listed per page
PostSummaryWords | How may words to show in post list summary
ImageCacheSlidingExpirationMinutes | Time for cached images to expire
EnableImageLazyLoad | Use lazy load to show images when user scrolls the page
UsePictureInsteadOfNotFoundResult | Show a friendly 404 picture or not
EnablePingBackReceive | Can blog receive pingback requests
EnablePingBackSend | Can blog send pingback to another blog
EnableHarmonizor | Filter bad words (in order to live in China)
EnableReward | Show WeChat reward button and QR Code image
EnforceHttps | Force website use HTTPS
DisableEmailSendingInDevelopment | When debugging locally, do not send email for real

### URL Rewrite

The only built-in rule is removing trailing slash in URLs. For other rules, you can customize by editing "\src\Moonglade.Web\urlrewrite.xml" according to [IIS URL Rewrite Module configuration](https://www.iis.net/downloads/microsoft/url-rewrite)

### FAQ

**Does this blog support upgrade from a lower version?**

It depends on whether the database schema is updated. If the schema is same for a higer version, then the system can be deployed and override old files without problem. If schema changes, you will need to execute **migration.sql** along with the deployment.

**How and why is this blog coupled with Microsoft Azure?**

Azure AD Authentication is the ONLY piece currently coupled with 
Azure, once local authentication provider is implemented, **this blog system will decouple with Azure**. For other part of the blog system, like Image Storage, you don't have to use Azure already. But the entire system works best on Azure.

### Optional Recommendations
- [Microsoft Azure DNS Zones](https://azure.microsoft.com/en-us/services/dns/)
- [Microsoft Azure App Service](https://azure.microsoft.com/en-us/services/app-service/)
- [Microsoft Azure SQL Database](https://azure.microsoft.com/en-us/services/sql-database/)
- [Microsoft Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/)

### Related Projects

Below open source projects are reusable components (NuGet packages) used in my blog, and they can be used in other websites as well. 

- [Edi.Blog.Pingback](https://github.com/EdiWang/Edi.Blog.Pingback)
- [Edi.Blog.OpmlFileWriter](https://github.com/EdiWang/Edi.Blog.OpmlFileWriter)
- [Edi.Captcha.AspNetCore](https://github.com/EdiWang/Edi.Captcha.AspNetCore)
- [Edi.ImageWatermark](https://github.com/EdiWang/Edi.ImageWatermark)
- [Edi.Net.AesEncryption](https://github.com/EdiWang/Edi.Net.AesEncryption)
- [Edi.Practice.RequestResponseModel](https://github.com/EdiWang/Edi.Practice.RequestResponseModel)
- [Edi.SyndicationFeedGenerator](https://github.com/EdiWang/Edi.SyndicationFeedGenerator)
- [Edi.TemplateEmail](https://github.com/EdiWang/Edi.TemplateEmail)
- [Edi.WordFilter](https://github.com/EdiWang/Edi.WordFilter)