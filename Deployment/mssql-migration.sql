-- v14.3.x - v14.4.0
CREATE TABLE [dbo].[LoginHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LoginTimeUtc] [datetime] NOT NULL,
	[LoginIp] [nvarchar](64) NULL,
	[LoginUserAgent] [nvarchar](128) NULL,
	[DeviceFingerprint] [nvarchar](128) NULL,
 CONSTRAINT [PK_LoginHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

DROP TABLE [LocalAccount]
GO