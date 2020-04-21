# 🌕 Moonglade

[![Build Status](https://dev.azure.com/ediwang/Edi-GitHub/_apis/build/status/EdiWang.Moonglade?branchName=master)](https://dev.azure.com/ediwang/Edi-GitHub/_build/latest?definitionId=68&branchName=master)

The [**.NET Core**](https://dotnet.microsoft.com/) blog system of [**edi.wang**](https://edi.wang) that runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/)

![image](https://blog.ediwangcdn.com/web-assets/ediwang-azure-arch-v2.png)

## 🎉 Features

**Basic:** Post, Comment, Category, Archive, Tag, Page, Friendlink

**Misc:** Pingback, RSS/Atom/OPML, Open Search, Reader View

## 🛠 Build and Run

Tools | Alternative
--- | ---
[.NET Core 3.1 SDK](http://dot.net) | N/A
[Visual Studio 2019](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/)
[Azure SQL Database](https://azure.microsoft.com/en-us/services/sql-database/) | [SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-2019) / LocalDB (Dev Only)

###  Setup Database

Development | Production 
--- | ---
Create an [SQL Server 2019 LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?WT.mc_id=AZ-MVP-5002809&view=sql-server-ver15) database. e.g. ```moonglade-dev``` | [Create an Azure SQL Database](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-database-get-started?WT.mc_id=AZ-MVP-5002809) or a SQL Server 2019 database. e.g. ```moonglade-production```
Update the ```MoongladeDatabase``` as your database connection string in **appsettings.Development.json** | Set environment variable: ```ConnectionStrings__MoongladeDatabase``` as your connection string. 

##### Connection String Example

*If you are deploying to Azure App Service, you can set the connection string in the Configuration blade.*

```json
"MoongladeDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=moonglade;Trusted_Connection=True;"
```

### 🔨 Build Source

Build and run ```./src/Moonglade.sln```
- Default Admin Username: ```admin```
- Default Admin Password: ```admin123```

## ⚙ Configuration

> Below section discuss system settings in **appsettings.[env].json**. For blog settings, please use "/admin/settings" UI.

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

#### 📂 File System (Alternative)

```json
"Provider": "filesystem",
"FileSystemSettings": {
  "Path": "${basedir}\\UploadedImages"
}
```
The ```Path``` can be relative or absolute. ```"$\{basedir\}"``` represents the website's current directory. Storing images files under website directory is **NOT** recommended. 

#### ☁ CDN

If ```GetImageByCDNRedirect``` is set to ```true```, the blog will get images from client browser using a 302 redirect. This is especially useful when you have a CDN for your image resources, like what I did on Azure. 

```json
"CDNSettings": {
    "GetImageByCDNRedirect": true,
    "CDNEndpoint": "https://ews.azureedge.net/ediwang-images"
}
```

### 📧 Email Notification

If you need email notification for new comments, new replies and pingbacks, you have to setup the Moonglade.Notification API first. See https://github.com/EdiWang/Moonglade.Notification for instructions.

```json
"Notification": {
  "Enabled": true,
  "ApiEndpoint": "{PROD-ENV-VARIABLE}",
  "ApiKey": "{PROD-ENV-VARIABLE}"
}
```

### 🖥 System Setttings

Key | Data Type | Description
--- | --- | ---
Editor | ```string``` | HTML / Markdown
CaptchaSettings:ImageWidth | ```int``` | Pixel Width of Captcha Image
CaptchaSettings:ImageHeight | ```int``` | Pixel Height of Captcha Image
PostAbstractWords | ```int``` | How may words to show in post list abstract
ImageCacheSlidingExpirationMinutes | ```int``` | Time for cached images to expire
EnforceHttps | ```bool``` | Force website use HTTPS
AllowScriptsInCustomPage | ```bool``` | Allow JavaScript in Page content or not
EnableAudit | ```bool``` | Enable Audit Log or not
EnablePostRawEndpoint | ```bool``` | Enable ```/meta``` and ```/content``` endpoint for post URL

## 🙄 FAQ

### Does this blog coupled with Microsoft Azure?

No, the system design does not couple with Azure, but the blog works best on Azure. Every part of the system, like Authentication and Image Storage, can be configured to use non-Azure options.