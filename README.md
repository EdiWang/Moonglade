# Project "Moonglade"

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade-Master-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=50)

Moonglade is the new blog system for https://edi.wang, It is a complete rewrite of the old system using [**.NET Core**](https://dotnet.microsoft.com/) and runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/).

![image](https://ediwangstorage.blob.core.windows.net/web-assets/ediwang-azure-arch.png?date=20190413)

## Features

**Basic:** Post, Comment, Category, Archive, Tag, Friendlink

**Misc:** Pingback, RSS/Atom/OPML, Open Search, Reader View

## Caveats

This is **NOT a general purpose blog system** like WordPress or other CMS. Currently it contains content "hard coded" for https://edi.wang. To make it yours, you will need to change a certain amount of code.

*I am generalizing the system piece by piece. But there are no specific plans and scopes currently.*

## Build and Run

### Tools and Dependencies
- [.NET Core 2.2 SDK](http://dot.net)
- [Visual Studio 2019](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Azure SQL Database](https://azure.microsoft.com/en-us/services/sql-database/) or [SQL Server 2017](https://www.microsoft.com/en-us/sql-server/sql-server-2017)
- [Microsoft Azure Subscription](https://azure.microsoft.com/)

### Setup Azure Active Directory

This blog is using [Azure AD](https://azure.microsoft.com/en-us/services/active-directory/) to sign in to admin portal. Local authentication provider is pretending to be implmented. 

Register an App in **Azure Active Directory**
- Set Redirection URI to **"https://localhost:5001/signin-oidc"**
- Add Redirection URI for your other domain names if needed.
- Copy "**appId**" to set as **AzureAd:ClientId** in appsettings.[env].json file

Example Reply URL Configuration
```
"replyUrlsWithType": [
{
    "url": "https://localhost:5001/signin-oidc",
    "type": "Web"
},
{
    "url": "https://edi.wang/signin-oidc",
    "type": "Web"
}]
```

### Setup Database

1. [Create an Azure SQL Database](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-database-get-started) or a SQL Server 2017+ database 

2. Execute script  **"Database\schema-mssql-140.sql"** 

*You may need to grant permission to the database for your machine or service account depends on your server configuration*

### Build Source

1. Create a "**appsettings.Development.json**" under "**src\Moonglade.Web**", this file defines development time settings such as accounts, db connections, keys, etc. It is by default ignored by git, so you will need to manange it on your own.

2. Update the connection string "**MoongladeDatabase**" in **appsettings.[env].json** according to your database configuration.

3. Build and run **Moonglade.sln**, startup project is **Moonglade.Web**

### Configuration

Below section discuss system settings in **appsettings.[env].json**. For blog settings, please use "/admin/settings" UI.

#### Email Password Encryption

**Encryption** controls the **IV** and **Key** for encrypted email passwords in database. 

*The blog will try to generate a pair of Key and IV on first run, and write values into appsettings.**[Current Environment]**.json only. This means the application directory **must NOT be read only**. You'll have to set keys manully if you must use a read only deployment.*

To get a random generated key, access URL "/admin/settings/generate-new-aes-keys".

#### Image Storage
**AppSettings:ImageStorage** controls how blog post images are stored. There are 2 built in options:

**Preferred: Azure Blob Storage**

You need to create an [**Azure Blob Storage**](https://azure.microsoft.com/en-us/services/storage/blobs/) with container level permission. 
```
"Provider": "azurestorage"
"AzureStorageSettings": {
  "ConnectionString": "YOUR CONNECTION STRING",
  "ContainerName": "YOUR CONTAINER NAME"
},
```

**Alternative: File System**

```
"Provider": "filesystem",
"FileSystemSettings": {
  "Path": "${basedir}\\UploadedImages"
}
```
The Path can be relative or absolute. "$\{basedir\}" represents the website's current directory. Storing images files under website directory is not recommended. 

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
RecentCommentsListSize | How many comments to show on side bar
EnforceHttps | Force website use HTTPS
DisableEmailSendingInDevelopment | When debugging locally, do not send email for real

### URL Rewrite

The only built-in rule is removing trailing slash in URLs. For other rules, you can customize by editing "\src\Moonglade.Web\urlrewrite.xml" according to [IIS URL Rewrite Module configuration](https://www.iis.net/downloads/microsoft/url-rewrite)

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