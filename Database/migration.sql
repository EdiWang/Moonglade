INSERT BlogConfiguration VALUES(5,'BloggerName','Edi Wang', GETDATE())
INSERT BlogConfiguration VALUES(99,'GeneralSettings',N'{"SiteTitle":"Edi.Wang","LogoText":"edi.wang","MetaKeyword":"edi.wang, Edi Wang, 汪宇杰","Copyright":"&copy; 2019 - 2019 edi.wang"}', GETDATE())

DELETE FROM BlogConfiguration WHERE CfgKey IN ('SiteTitle', 'MetaKeyword')