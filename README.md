# Moonglade Blog

Moonglade is a personal blogging platform optimized for deployment on [**Microsoft Azure**](https://azure.microsoft.com/en-us/). It is designed for developers, offering essential blogging features such as posts, comments, categories, archives, tags, and pages.

## 📦 Deployment Notice

- **Stable Code:** Always use the stable code from the [Release](https://github.com/EdiWang/Moonglade/releases) branch instead of the master branch.

- **Security:** Enable HTTPS and HTTP/2 on your web server for enhanced security and performance.

- **Deployment Options:** While Azure is recommended, Moonglade Blog can also be deployed on other cloud providers or on-premises environments.

- **Regulation:** Due to regulation concerns in China, Moonglade will run in read-only mode when deployed in China. If you are in China, please consider another blogging platform.

### Full Deploy on Azure

This is the way https://edi.wang is deployed, by taking advantage of as many Azure services as possible, the blog can run very fast and secure. There is no automated script to deploy it, you need to manually create all the resources.

![image](https://cdn.edi.wang/web-assets/ediwang-azure-arch-visio-oct2024.svg)

### Quick Deploy on Azure (App Service on Linux)

Use automated deployment script to get your Moonglade up and running in 10 minutes with minimal Azure components, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure)

## 🐵 Development

Tools | Alternative
--- | ---
[Visual Studio 2022](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/) with [.NET 8.0 SDK](http://dot.net)
[SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-2022) | [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver16&WT.mc_id=AZ-MVP-5002809), PostgreSQL or MySQL 

### Setup Database

> Free version of SQL Server Express would be sufficient for production use.

Database | `appsettings.json/ConnectionStrings/MoongladeDatabase` Example
--- | ---
Microsoft SQL Server | `Server=(local);Database=moonglade;Trusted_Connection=True;`
MySQL | `Server=localhost;Port=3306;Database=moonglade;Uid=root;Pwd=***;`
PostgreSQL | `User ID=***;Password=***;Host=localhost;Port=5432;Database=moonglade;Pooling=true;`

### Build Source

Build and run `./src/Moonglade.sln`
- Home page: `https://localhost:17251`
- Admin: `https://localhost:17251/admin`
  - Default username: `admin`
  - Default password: `admin123`

## ⚙ Configuration

> This section is for **appsettings.json**. For other blog settings, please use "/admin/settings" UI.

### Authentication

Moonglade is using local account by default, you can manage accounts in `/admin/settings/account`. You can also use  [Microsoft Entra ID](https://azure.microsoft.com/en-us/services/active-directory/) to login. See [Wiki document](https://github.com/EdiWang/Moonglade/wiki/Use-Microsoft-Entra-ID-Authentication) for setup Microsoft Entra ID.

### Image Storage
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

#### [Minio Blob Storage](https://min.io/)

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

#### File System (Not Recommended)

You can also choose File System for image storage if you don't have a cloud option.

Windows Deployment Example:

```json
{
  "Provider": "filesystem",
  "FileSystemPath": "C:\\UploadedImages"
}
```

Linux Deployment Example:

```json
{
  "Provider": "filesystem",
  "FileSystemPath": "/var/UploadedImages"
}
```

### Comment Moderator

Setup [Moonglade.ContentSecurity](https://github.com/EdiWang/Moonglade.ContentSecurity)  Azure Function to enable comment moderation.

```json
"ContentModerator": {
  "Provider": "",
  "ApiEndpoint": "",
  "ApiKey": ""
}
```

### Email Notification

Setup [Moonglade.Email](https://github.com/EdiWang/Moonglade.Email) Azure Function to enable email notification for new comments, new replies, webmentions and pingbacks. Then enable notification in admin portal.

```json
"Email": {
  "ApiEndpoint": "",
  "ApiKey": ""
}
```

### Others

- [System Settings](https://github.com/EdiWang/Moonglade/wiki/System-Settings)
- [Security HTTP Headers](https://github.com/EdiWang/Moonglade/wiki/Security-Headers)

## 🎉 Protocols or Standards

Name | Feature | Status | Service Endpoint
--- | --- | --- | ---
RSS | Subscription | Supported | `/rss`
Atom | Subscription | Supported | `/atom`
OPML | Subscription | Supported | `/opml`
Open Search | Search | Supported | `/opensearch`
Pingback | Social | Supported | `/pingback`
Webmention | Social | Supported | `/webmention`
Reader View | Reader mode | Supported | N/A
FOAF | Social | Supported | `/foaf.xml`
IndexNow | SEO | Supported | N/A
RSD | Service Discovery | Deprecated | N/A
MetaWeblog | Blogging | Deprecated | N/A
Dublin Core Metadata | SEO | Basic Support | N/A

## 免责申明

对于中国访客，我们有一份特供的免责申明。请确保你已经阅读并理解其内容：[免责申明（仅限中国访客）](./DISCLAIMER_CN.md)
