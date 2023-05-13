# Moonglade Blog

[![Build Status](https://dev.azure.com/ediwang/Edi-GitHub/_apis/build/status/EdiWang.Moonglade?branchName=master)](https://dev.azure.com/ediwang/Moonglade%20DevOps/_build/latest?definitionId=68&branchName=master) 
[![Docker Linux x64](https://github.com/EdiWang/Moonglade/actions/workflows/docker.yml/badge.svg)](https://github.com/EdiWang/Moonglade/actions/workflows/docker.yml)
![.NET Build Linux](https://github.com/EdiWang/Moonglade/workflows/.NET%20Build%20Linux/badge.svg) 

The [.NET](https://dotnet.microsoft.com/) blog system of [edi.wang](https://edi.wang) that runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/). Designed for developers, enabling most common blogging features including posts, comments, categories, archive, tags and pages.

## 📦 Deployment

- It is recommended to use stable code from [Release](https://github.com/EdiWang/Moonglade/releases) rather than master branch.

- It is recommended to enable HTTP/2 support on your web server.

### ☁ Full Deploy on Azure (Recommend)

This is the way https://edi.wang is deployed, by taking advantage of as many Azure services as possible, the blog can run very fast and secure.

This diagram shows a full Azure deployment for Moonglade for reference.

![image](https://cdn-blog.edi.wang/web-assets/ediwang-azure-arch-visio-nov2022.png)

### 🐋 Quick Deploy on Azure

Use automated deployment script to get your Moonglade up and running in 10 minutes, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure)

### 🐧 Quick Deploy on Linux without Docker

To quickly get it running on a new Linux machine without Docker, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Install-on-Linux-Machine). You can watch video tutorial [here](https://anduins-site.player.aiur.site/moonglade-install.mp4).

## 🐵 Development

Tools | Alternative
--- | ---
[Visual Studio 2022 v17.4+](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/) with [.NET 7.0 SDK](http://dot.net)
[SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-2022) | [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver16&WT.mc_id=AZ-MVP-5002809), PostgreSQL or MySQL 

### 💾 Setup Database

Moonglade supports three types of database. You can choose from SQL Server, PostgreSQL or MySQL.

#### SQL Server

Create a SQL Server 2022 database, e.g. ```moonglade```

Set the `MoongladeDatabase` to your database connection string in `appsettings.Development.json`

```json
"MoongladeDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=moonglade;Trusted_Connection=True;"
```

#### MySQL

Set `DatabaseType` to `MySql`

```json
"DatabaseType": "MySql"
```

Set the `MoongladeDatabase` to your database connection string in `appsettings.Development.json`

```json
"MoongladeDatabase": "Server=localhost;Port=3306;Database=moonglade;Uid=root;Pwd=******;"
```

#### PostgreSql

Set `DatabaseType` to `PostgreSql`

```json
"DatabaseType": "PostgreSql"
```

Set the `MoongladeDatabase` to your database connection string in `appsettings.Development.json`

```json
"MoongladeDatabase": "User ID=****;Password=****;Host=localhost;Port=5432;Database=****;Pooling=true;"
```

### 🔨 Build Source

Build and run `./src/Moonglade.sln`
- Admin: `https://localhost:1055/admin`
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
`ImageStorage` controls how blog post images are stored.

#### [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) (Preferred)

You need to create an [**Azure Blob Storage**](https://azure.microsoft.com/en-us/services/storage/blobs/) with **container level permission**. 

```json
{
  "Provider": "azurestorage"
  "AzureStorageSettings": {
    "ConnectionString": "YOUR CONNECTION STRING",
    "ContainerName": "YOUR CONTAINER NAME"
  }
}
```

When configured the image storage to use Azure Blob, you can take advantage of CDN for your image resources. Just enable CDN in admin settings, the blog will get images from CDN.

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

#### [Qiniu Blob Storage](https://qiniu.com/) (Almost free)

You need to hava an Qiniu cloud account, and use [Kodo](https://www.qiniu.com/products/kodo) storage service. 

```json
"Provider": "qiniustorage"
"QiniuStorageSettings": {
  "EndPoint": "Your Custom Domain",
  "AccessKey": "Your Access Key",
  "SecretKey": "Your Secret Key",
  "BucketName": "Your BucketName",
  "WithSSL": false
}
```

#### File System (Not Recommended)

You can also choose File System for image storage if you don't have a cloud option.

```json
{
  "Provider": "filesystem",
  "FileSystemPath": "C:\\UploadedImages"
}
```

### 🤬 Comment Moderator

- [Comment Moderator Settings](https://github.com/EdiWang/Moonglade/wiki/Comment-Moderator-Settings)

### 📧 Email Notification

If you need email notification for new comments, new replies and pingbacks, you have to setup the [Moonglade.Notification Azure Function](https://github.com/EdiWang/Moonglade.Notification) first, and then enable notification in admin portal.

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
- [X] FOAF
- [X] RSD
- [X] MetaWeblog (Basic Support)
- [X] Dublin Core Metadata (Basic Support)
- [ ] BlogML - Under triage
- [ ] APML - Not planned
- [ ] Trackback - Not planned

## 🐼 Example Blogs

There are a few individuals already setup thier blogs using Moonglade on Azure (Global or China), Alibaba Cloud, Tencent Cloud, etc.

- [zchwei](https://zchwei.com/)
- [yycoding](https://www.yycoding.xyz/)
- [51azure](https://www.51azure.cloud/)
- [Zhuangkh](https://zhuangkh.com/)
- [HueiFeng](https://blog.stackable.cn/)
- [Leslie Wang](https://lesliewxj.com/)
- [AllenMasters](https://allenmasters.com)
- [Hao's House](https://haxu.dev/)
- [Sascha.Manns](https://saschamanns.de/)

*Just Submit PR or issue if you want your blog to be listed here*
