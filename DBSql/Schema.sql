CREATE TABLE [dbo].[Posts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
     CONSTRAINT PK_Posts PRIMARY KEY CLUSTERED ([Id] ASC)
    );
GO
CREATE TABLE [dbo].[Users](
    [Userid] INT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [Fullname] NVARCHAR(150) NOT NULL,
    [Password] NVARCHAR(255) NOT NULL,

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED ([Userid] ASC)
);
GO
CREATE TABLE [dbo].[RefreshToken]
(
    [Token] NVARCHAR(500) NOT NULL,
    [UserId] INT NOT NULL,
    [ExpiresAt] DATETIME2(7) NOT NULL,
    [IsRevoked] BIT NOT NULL CONSTRAINT DF_RefreshToken_IsRevoked DEFAULT (0),

    CONSTRAINT [PK_RefreshToken] 
        PRIMARY KEY CLUSTERED ([Token] ASC),

    CONSTRAINT [FK_RefreshToken_Users] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[Users]([UserId])
        ON DELETE CASCADE
);
GO
