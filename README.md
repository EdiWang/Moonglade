# Project "Moonglade"

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade-Master-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=50)

This is the new blog system for https://edi.wang, Moonglade is the successor of project "Nordrassil", which was the .NET Framework version of the blog system. Moonglade is a complete rewrite of the old system using **.NET Core**, optimized for cloud-based hosting.

![image](https://ediwangstorage.blob.core.windows.net/web-assets/ediwang-azure-arch.png?date=20190413)

## Features

**Basic:** Post, Comment, Category, Archive, Tag, Friendlink

**Misc:** Pingback, RSS/Atom/OPML, Open Search, Reader View

## Caveats

This is **NOT a general purpose blog system** like WordPress or other CMS. Currently it contains content "hard coded" for https://edi.wang.

To make it yours, you will need to change a certain amount of code.

I am looking into generalize the system in the long term. But there are no specific plans and scopes for the currently. You are welcomed to raise PR to move out the "edi.wang" specific code.

## Build and Run

Current code is not so setup-friendly, it is very complicated comparing to other blog or CMS systems, I am consider improving the setup steps.

### Tools and Dependencies
- [.NET Core 2.2 SDK](http://dot.net)
- [Visual Studio 2019](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- SQL Server 2017 / Azure SQL Database
- Microsoft Azure Active Directory

### Setup Azure AD

This blog is using Azure AD to sign in to admin portal. Local authentication provider is not implmented yet. 

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

1. Create a SQL Server 2017+ database or Azure SQL Database

2. Execute script  **"Database\schema-mssql-140.sql"** 

*You may need to grant permission to the database for your machine or service account depends on your server configuration*

### Build Source

1. Create a "**appsettings.Development.json**" under "**src\Moonglade.Web**", this file defines development time settings such as accounts, db connections, keys, etc. It is by default ignored by git, so you will need to manange it on your own.

2. Update the connection string "**MoongladeDatabase**" in **appsettings.[env].json** according to your database configuration.

3. Build and run **Moonglade.sln**, startup project is **Moonglade.Web**

### Configuration

#### Email Password Encryption

**Encryption** controls the **IV** and **Key** for encrypted email passwords in database. 

*The blog will try to generate a pair of Key and IV on first run, and write values into appsettings.**[Current Environment]**.json only. This means the application directory **must NOT be read only**. You'll have to set keys manully if you must use a read only deployment.*

To get a random generated key, access URL "/admin/settings/generate-new-aes-keys".

See [Edi.Net.AesEncryption](https://github.com/EdiWang/Edi.Net.AesEncryption) project for more information.

#### Image Storage
**AppSettings:ImageStorage** controls how blog post images are stored. There are 2 built in options:

1. **Azure Blob:** You need to create an **Azure Storage Account** with a blob container. 
```
"Provider": "AzureStorageImageProvider"
"AzureStorageSettings": {
  "ConnectionString": "YOUR CONNECTION STRING",
  "ContainerName": "YOUR CONTAINER NAME"
},
```

2. **File System:** Set provider to **FileSystemImageProvider**
```
"Provider": "FileSystemImageProvider",
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

## Host on Server

You can host Moonglade on public internet.

### Server Requirments

- Windows or Linux Servers that supports .NET Core 2.2
- A Microsoft Azure subscription, for setup Azure AD Authentication

### Web Server Configuration

#### Email Notification

To enable email notifications such as new comments, pingback requests, set up in the blog admin portal.

### Optional Recommendations
- Microsoft Azure DNS Zones
- Microsoft Azure App Service
- Microsoft Azure SQL Database
- Microsoft Azure Blob Storage