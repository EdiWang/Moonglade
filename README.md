# Project "Moonglade"

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade-Master-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=50)

This is the new blog system for https://edi.wang, Moonglade project is the successor of project "Nordrassil", which was the .NET Framework version of the blog system. Moonglade is a complete rewrite of the old system using **.NET Core**, focus on performance and optimized for cloud-based hosting.

## Blog Features
- Post
- Comment
- Category
- Tag
- Pingback
- RSS/Atom/OPML
- Open Search

## Technologies Stack

// TODO: Use picture

## Build and Run

### Tools
- [.NET Core 2.2 SDK](http://dot.net)
- [Visual Studio 2017](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- SQL Server 2017+ / Azure SQL Database

### Prepare Azure AD

This blog is using Azure AD to sign in to admin portal. Local authentication provider is not implmented yet. So yes, basically you have to use Azure currently.

Register an App in **Azure Active Directory**
- Set Redirection URI to "https://localhost:5001/signin-oidc"
- Copy "**appId**" to set as **AzureAd:ClientId** in later appsettings file

### Build Source

1. Create a "**appsettings.Development.json**" under "src\Moonglade.Web", this file defines development time settings such as accounts, db connections, keys, etc. It is by default ignored by git, so you will need to manange it on your own.

2. Create a SQL Server dabase using "**Database\schema-mssql-140.sql**", and update the connection string "**MoongladeDatabase**" in **appsettings.Development.json**. 

3. Build and run **Moonglade.sln**

### Configuration

#### Image Storage
**AppSettings:ImageStorage** controls how blog post images are stored. There are 2 built in options:

1. Azure: You need to create an **Azure Storage Account** with a blob container. 
```
"Provider": "AzureStorageImageProvider"
"AzureStorageSettings": {
  "ConnectionString": "YOUR CONNECTION STRING",
  "ContainerName": "YOUR CONTAINER NAME"
},
```

2. File System: *WANING: This provider code has only been smoke tested on my own machine*
```
"Provider": "FileSystemImageProvider",
"FileSystemSettings": {
  "Path": "${basedir}\\UploadedImages"
}
```

#### Email Password Encryption

**Encryption** controls the **IV** and **Key** for encrypted email passwords in database. 

See [Edi.Net.AesEncryption](https://github.com/EdiWang/Edi.Net.AesEncryption) project for more information.

#### Others

Key | Description
--- | ---
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

## Host on Server

Windows or Linux Servers that supports .NET Core 2.2

### Required
- Microsoft Azure Active Directory

### Recommended
- Microsoft Azure DNS Zones
- Microsoft Azure App Service
- Microsoft Azure SQL Database
- Microsoft Azure Blob Storage