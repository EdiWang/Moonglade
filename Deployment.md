## Quick Install on Linux Machine

### Brief steps:

* Get a domain name. (Like `moonglade.example.com`)
* Get a server.
* Install on your server.

### Get a server

Get a brand new Ubuntu 18.04 server.

  * Server must be Ubuntu 18.04. (20.04 and 16.04 is not supported)
  * Server must have a public IP address. (No local VM)
  * Server must have access to the global Internet. (Not Chinese network)

Vultr or DigitalOcean is recommended besides Azure.

### Install on server

Execute the following command on the server:

```bash
$ curl -sL https://go.edi.wang/aka/mgli | sudo bash -s moonglade.example.com
```

**By install, you accept the eula of SQL Server: https://go.microsoft.com/fwlink/?LinkId=2104294&clcid=0x409**

To uninstall:

```bash
$ curl -sL https://go.edi.wang/aka/mglu | sudo bash
```
