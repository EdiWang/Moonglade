# 🌕 Moonglade

[![Build Status](https://dev.azure.com/ediwang/Edi-GitHub/_apis/build/status/EdiWang.Moonglade?branchName=master)](https://dev.azure.com/ediwang/Edi-GitHub/_build/latest?definitionId=68&branchName=master) ![Docker Build and Push](https://github.com/EdiWang/Moonglade/workflows/Docker%20Build%20and%20Push/badge.svg)

The [.NET Core](https://dotnet.microsoft.com/) blog system of [edi.wang](https://edi.wang) that runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/). Enable most common blogging features including Posts, Comments, Categories, Archive, Tags, Pages and Friendlink.

![image](https://blog.ediwangcdn.com/web-assets/ediwang-azure-arch-v4.png)

## 📦 Deployment

> The system design DOES NOT couple with Azure, but the blog works best on Azure. Every part of the system, like Authentication and Image Storage, can be configured to use non-Azure options.

### ☁ Full Deploy on Azure (Recommend)

This is the way https://edi.wang is deployed, by taking advantage of as many Azure services as possible, the blog can run very fast and secure with only ~$300 USD/month.

It is recommended to use stable code from [Release](https://github.com/EdiWang/Moonglade/releases) rather than master branch.

This diagram shows a recommended full feature Azure deployment for Moonglade. It doesn't come out of the box. Although the `./Deployment/AzureAppServiceDeploy.ps1` can cover a part of it, you have to manually setup every other piece of it. 

![image](https://blog.ediwangcdn.com/web-assets/ediwang-azure-arch-visio.png)

### 🐋 Quick Deploy on Azure with/out Docker 

If you just want to quickly get it running on Azure without knowing every detail. You can have a minimal deployment that use Docker Container to run on App Service (Linux) by executing the quick start deployment script in PowerShell Core:

`./Deployment/AzureAppServiceDeploy.ps1`

Please edit the script file and replace these items with your own values:

```powershell
# Replace with your own values
$subscriptionName = "Microsoft MVP"
$rsgName = "Moonglade-Test-RSG"
$regionName = "East Asia"
$webAppName = "moonglade-test-web"
$aspName = "moonglade-test-plan"
$storageAccountName = "moongladeteststorage"
$storageContainerName = "moongladetestimages"
$sqlServerName = "moongladetestsqlsvr"
$sqlServerUsername = "moonglade"
$sqlServerPassword = "DotNetM00n8!@d3"
$sqlDatabaseName = "moonglade-test-db"
$cdnProfileName = "moonglade-test-cdn"
[bool] $useLinuxPlanWithDocker = 1
```

Set `$useLinuxPlanWithDocker` to `1` will use Docker on Linux App Service plan, it will be a ready to run deployment. Set it to `0` will only deploy infrastructure without the application code, and leave the deployment in your control.

### 🐧 Quick Deploy on Linux

If you just want to quickly get it running on a new Linux machine without Docker, please follow the steps [here](./Deployment.md).

## 🐵 Development

Tools | Alternative
--- | ---
[.NET Core 3.1 SDK](http://dot.net) | N/A
[Visual Studio 2019](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/)
[SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-2019) / LocalDB | N/A

### 💾 Setup Database

Create an [SQL Server 2019 LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15?WT.mc_id=AZ-MVP-5002809) database. e.g. ```moonglade```

Update the ```MoongladeDatabase``` as your database connection string in **appsettings.Development.json** or set environment variable: ```ConnectionStrings__MoongladeDatabase``` as your connection string. 

Example

```json
"MoongladeDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=moonglade;Trusted_Connection=True;"
```

*If you are deploying to Azure App Service, you can set the connection string in the Configuration blade.*

### 🔨 Build Source

Build and run ```./src/Moonglade.sln```
- Default Admin Username: ```admin```
- Default Admin Password: ```admin123```

## ⚙ Configuration

> This section discuss system settings in **appsettings.[env].json**. For blog settings, please use "/admin/settings" UI.

**For production, it is strongly recommended to use Environment Variables over appsetting.json file.**

### 🛡 Authentication

#### [Azure Active Directory]((https://azure.microsoft.com/en-us/services/active-directory/)) (Preferred)

- Register an App in **Azure Active Directory**
- Set Redirection URI to **"https://yourdomain/signin-oidc"** (For local debugging, also add URL to https://localhost:1055/signin-oidc)
- Check `ID Tokens` checkbox under 'Advanced settings'.
- Copy ```appId``` to set as ```AzureAd:ClientId``` in **appsettings.[env].json** file

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

#### Local Account (Alternative)

Set ```Authentication:Provider``` to ```"Local"``` and assign a pair of username and password. 

*Password is not encrypted, use it at your own risk.*

```json
"Authentication": {
  "Provider": "Local",
  "Local": {
    "Username": "admin",
    "Password": "admin123",
  }
}
```

### 🖼 Image Storage
```AppSettings:ImageStorage``` controls how blog post images are stored.

#### [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) (Preferred)

You need to create an [**Azure Blob Storage**](https://azure.microsoft.com/en-us/services/storage/blobs/) with **container level permission**. 

```json
"Provider": "azurestorage"
"AzureStorageSettings": {
  "ConnectionString": "YOUR CONNECTION STRING",
  "ContainerName": "YOUR CONTAINER NAME"
}
```

When configured the image storage to use Azure Blob, you can take advantage of CDN for your image resources. Set ```GetImageByCDNRedirect``` to ```true```, the blog will get images from client browser using a 302 redirect. 

```json
"CDNSettings": {
    "GetImageByCDNRedirect": true,
    "CDNEndpoint": "https://yourendpoint.azureedge.net/moonglade-images"
}
```

#### File System (Not Recommended)

You can also choose File System for image storage, but this will make your site root not read-only, which would be a potential security issue. And it will be harder for you to backup or update the website.

```json
"Provider": "filesystem",
"FileSystemSettings": {
  "Path": "${basedir}\\UploadedImages"
}
```
The ```Path``` can be relative or absolute. ```"$\{basedir\}"``` represents the website's current directory. 

### 📧 Email Notification

If you need email notification for new comments, new replies and pingbacks, you have to setup the [Moonglade.Notification Azure Function](https://github.com/EdiWang/Moonglade.Notification) first, and then set the values in ```appsettings.[env].json``` or in your runtime environment variables.

```json
"Notification": {
  "Enabled": true,
  "AzureFunctionEndpoint": "{PROD-ENV-VARIABLE}"
}
```

### 🖥 System Setttings

Key | Data Type | Description
--- | --- | ---
```AllowExternalScripts``` | ```bool``` | If CSP should enable external JavaScript links
```CaptchaSettings:ImageWidth``` | ```int``` | Pixel Width of Captcha Image
```CaptchaSettings:ImageHeight``` | ```int``` | Pixel Height of Captcha Image
```DefaultLangCode``` | ```string``` | Default language code for editing posts (e.g. ```en-us```)
```Editor``` | ```string``` | ```HTML``` or ```Markdown```
```EnforceHttps``` | ```bool``` | Force website use HTTPS
```EnableAudit``` | ```bool``` | Enable Audit Log or not
```EnableOpenGraph``` | ```bool``` | Enable Open Graph
```EnableWebApi``` | ```bool``` | Enable REST API
```CacheSlidingExpirationMinutes:Post``` | ```int``` | Time for cached posts to expire
```CacheSlidingExpirationMinutes:Page``` | ```int``` | Time for cached pages to expire
```CacheSlidingExpirationMinutes:Image``` | ```int``` | Time for cached image to expire
```PostAbstractWords``` | ```int``` | How may words to show in post list abstract
```SystemNavMenus:Categories``` | ```bool``` | Show 'Categories' Menu
```SystemNavMenus:Tags``` | ```bool``` | Show 'Tags' Menu
```SystemNavMenus:Archive``` | ```bool``` | Show 'Archive' Menu

## 🎉 Blog Protocols or Standards

- [X] RSS
- [X] Atom
- [X] OPML
- [X] Open Search
- [X] Pingback
- [X] Reader View
- [ ] APML - Not planned
- [ ] FOAF - Under triage
- [ ] BlogML - Under triage
- [ ] Trackback - Not planned
- [ ] RSD - Not planned
- [ ] MetaWeblog - Not planned

## 🐼 Customers

There are a few individuals already setup thier blogs using Moonglade on Azure (Global or China), Alibaba Cloud, Tencent Cloud, etc.

- [Anduin Xue](https://anduin.aiursoft.com/)
- [zchwei](https://zchwei.com/)
- [yycoding](https://www.yycoding.xyz/)
- [51azure](https://www.51azure.cloud/)
- [Zhuangkh](https://zhuangkh.com/)
- [HueiFeng](https://blog.stackable.cn/)

*Just Submit PR or issue if you want your blog to be listed here*
