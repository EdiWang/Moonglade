# 🌕 Moonglade

[![Build Status](https://dev.azure.com/ediwang/Edi-GitHub/_apis/build/status/EdiWang.Moonglade?branchName=master)](https://dev.azure.com/ediwang/Edi-GitHub/_build/latest?definitionId=68&branchName=master) 
![Code Coverage](https://img.shields.io/azure-devops/coverage/ediwang/Edi-GitHub/68)
![Docker Build and Push](https://github.com/EdiWang/Moonglade/workflows/Docker%20Build%20and%20Push/badge.svg) 
![.NET Build Linux](https://github.com/EdiWang/Moonglade/workflows/.NET%20Build%20Linux/badge.svg)

The [.NET](https://dotnet.microsoft.com/) blog system of [edi.wang](https://edi.wang) that runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/). Enabling most common blogging features including posts, comments, categories, archive, tags and pages.

## 📦 Deployment

> It is recommended to use stable code from [Release](https://github.com/EdiWang/Moonglade/releases) rather than master branch.

### ☁ Full Deploy on Azure (Recommend)

This is the way https://edi.wang is deployed, by taking advantage of as many Azure services as possible, the blog can run very fast and secure with only ~$300 USD/month.

This diagram shows a full Azure deployment for Moonglade for reference.

![image](https://blog.ediwangcdn.com/web-assets/ediwang-azure-arch-visio.png)

### 🐋 Quick Deploy on Azure

Use automated deployment script to get your Moonglade up and running in 10 minutes, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure)

### 🐧 Quick Deploy on Linux without Docker

To quickly get it running on a new Linux machine without Docker, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Install-on-Linux-Machine).

## 🐵 Development

Tools | Alternative
--- | ---
[Visual Studio 2019 v16.8+](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/) with [.NET 5.0 SDK](http://dot.net)
[SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-2019) | [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15?WT.mc_id=AZ-MVP-5002809)

### 💾 Setup Database

Create a SQL Server 2019 or LocalDB database. e.g. ```moonglade```

Update the `MoongladeDatabase` with your database connection string in `appsettings.Development.json`

```json
"MoongladeDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=moonglade;Trusted_Connection=True;"
```

### 🔨 Build Source

Build and run `./src/Moonglade.sln`
- Admin entrance: `/admin`
- Default username: `admin`
- Default password: `admin123`

## ⚙ Configuration

> This section discuss system settings in **appsettings.[env].json**. For blog settings, please use "/admin/settings" UI.

**For production, it is strongly recommended to use Environment Variables over appsetting.json file.**

### 🛡 Authentication

#### [Azure Active Directory](https://azure.microsoft.com/en-us/services/active-directory/)

See [Wiki document](https://github.com/EdiWang/Moonglade/wiki/Use-Azure-Active-Directory-Authentication)

#### Local Account (Alternative)

Set `Authentication:Provider` to `"Local"`. You can manage accounts in `/admin/settings/account`

### 🖼 Image Storage
`AppSettings:ImageStorage` controls how blog post images are stored.

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
    "EnableCDNRedirect": true,
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

#### [Minio Blob Storage](https://min.io/) (Free)

You need to hava an [**Minio Server**](https://docs.min.io/). 

```json
"Provider": "miniostorage"
"MinioStorageSettings": {
  "EndPoint": "Minio Server Endpoint(eg:localhost:9600)",
  "AccessKey": "Your Access Key",
  "SecretKey": "Your Secret Key",
  "BucketName": "Your BucketName",
  "WithSSL": false
}
```
### 🤬 Comment Moderator

You can control how comment content are moderated in `CommentModerator` section.

#### Local (Default)

Offline content moderator that look for bad words and apply policy to comments. You can set bad words and policy in admin portal.

```json
"CommentModerator": {
  "Provider": "Local"
}
```

#### Azure Content Moderator

Powered by AI, use Azure Cognitive Services to filter bad words in comments.

```json
"CommentModerator": {
  "Provider": "Azure",
  "AzureContentModeratorSettings": {
    "Endpoint": "<Your Azure Content Moderator endpoint>",
    "OcpApimSubscriptionKey": "<Your Azure Content Moderator key>"
  }
}
```

### 📧 Email Notification

If you need email notification for new comments, new replies and pingbacks, you have to setup the [Moonglade.Notification Azure Function](https://github.com/EdiWang/Moonglade.Notification) first, and then set the values in ```appsettings.[env].json``` or in your runtime environment variables.

```json
"Notification": {
  "Enabled": true,
  "AzureFunctionEndpoint": "{PROD-ENV-VARIABLE}"
}
```
### 🔩 Others

- [System Settings](https://github.com/EdiWang/Moonglade/wiki/System-Settings)
- [Security Headers (CSP, XSS, etc.)](https://github.com/EdiWang/Moonglade/wiki/Security-Headers-(CSP,-XSS,-etc.))

## 🎉 Blog Protocols or Standards

- [X] RSS
- [X] Atom
- [X] OPML
- [X] Open Search
- [X] Pingback
- [X] Reader View
- [ ] APML - Not planned
- [X] FOAF
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
- [Leslie Wang](https://lesliewxj.com/)

*Just Submit PR or issue if you want your blog to be listed here*
