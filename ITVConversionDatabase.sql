USE [ITVConversion]
GO
/****** Object:  Table [dbo].[ReportClassMapping]    Script Date: 21/03/2019 18:18:22 ******/
DROP TABLE [dbo].[ReportClassMapping]
GO
/****** Object:  Table [dbo].[ProviderContentTierMapping]    Script Date: 21/03/2019 18:18:22 ******/
DROP TABLE [dbo].[ProviderContentTierMapping]
GO
/****** Object:  Table [dbo].[MediaLocations]    Script Date: 21/03/2019 18:18:22 ******/
DROP TABLE [dbo].[MediaLocations]
GO
/****** Object:  Table [dbo].[ITV_Conversion_Data]    Script Date: 21/03/2019 18:18:22 ******/
DROP TABLE [dbo].[ITV_Conversion_Data]
GO
/****** Object:  Table [dbo].[FieldMappings]    Script Date: 21/03/2019 18:18:22 ******/
DROP TABLE [dbo].[FieldMappings]
GO
/****** Object:  Table [dbo].[FieldMappings]    Script Date: 21/03/2019 18:18:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FieldMappings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ADI_App_Type] [nvarchar](3) NOT NULL,
	[ADI_Element] [nvarchar](50) NOT NULL,
	[ITV_Element] [nvarchar](50) NOT NULL,
	[IsTitleMetadata] [bit] NOT NULL,
	[IsMandatoryField] [bit] NOT NULL,
 CONSTRAINT [PK_FieldMappings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ITV_Conversion_Data]    Script Date: 21/03/2019 18:18:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ITV_Conversion_Data](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PAID] [nvarchar](50) NULL,
	[Title] [nvarchar](250) NULL,
	[IsTVOD] [bit] NULL,
	[VersionMajor] [int] NULL,
	[IsAdult] [bit] NULL,
	[PublicationDate] [datetime] NULL,
	[LicenseStartDate] [datetime] NULL,
	[LicenseEndDate] [datetime] NULL,
	[ProviderName] [nvarchar](50) NULL,
	[ProviderId] [nvarchar](50) NULL,
	[Original_ITV] [nvarchar](max) NULL,
	[Original_ADI] [xml] NULL,
	[MediaFileName] [nvarchar](250) NULL,
	[MediaFileLocation] [nvarchar](max) NULL,
	[MediaChecksum] [nvarchar](250) NULL,
	[ProcessedDateTime] [datetime] NULL,
	[Updated_ITV] [nvarchar](max) NULL,
	[Update_ADI] [xml] NULL,
	[UpdatedFileName] [nvarchar](250) NULL,
	[UpdatedFileLocation] [nvarchar](250) NULL,
	[UpdatedMediaChecksum] [nvarchar](250) NULL,
	[UpdatedDateTime] [datetime] NULL,
 CONSTRAINT [PK_ITV_Conversion_Data] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MediaLocations]    Script Date: 21/03/2019 18:18:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MediaLocations](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MediaLocation] [nvarchar](max) NOT NULL,
	[DeleteFromSource] [bit] NOT NULL,
 CONSTRAINT [PK_MediaLocations] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProviderContentTierMapping]    Script Date: 21/03/2019 18:18:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProviderContentTierMapping](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Distributor] [nvarchar](50) NOT NULL,
	[Provider_Content_Tier] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Provider_Content_Tier_Mapping] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ReportClassMapping]    Script Date: 21/03/2019 18:18:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReportClassMapping](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Reporting_Class] [nvarchar](50) NOT NULL,
	[ClassIncludes] [nvarchar](max) NULL,
	[Folder_Location] [nvarchar](50) NOT NULL,
	[ShowType] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ReportClassMapping] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[FieldMappings] ON 

INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1, N'VOD', N'MSORating', N'ContentGuidance', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (2, N'VOD', N'Actors', N'Actors', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (3, N'VOD', N'Advisories', N'Advisories', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (4, N'VOD', N'Audience', N'ReportingClass', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (5, N'VOD', N'Billing_ID', N'BillingId', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (6, N'VOD', N'Broadcast_Date', N'CreationDate', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (7, N'VOD', N'Country_of_Origin', N'CountryOfOrigin', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (8, N'VOD', N'Director', N'Directors', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (9, N'PTP', N'Display_Provider', N'DisplayProvider', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (10, N'VOD', N'Distributor_Name', N'Distributor', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (11, N'VOD', N'Episode_ID', N'Episode_ID', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (12, N'VOD', N'Episode_Name', N'Episode_Name', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (13, N'VOD', N'Episode_Ordinal', N'Episode_Ordinal', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (14, N'VOD', N'External_Reference', N'ProviderAssetId', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (17, N'PTP', N'Folder_Location', N'ReportingClass', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (18, N'VOD', N'Genre', N'Genre', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (19, N'VOD', N'Licensing_Window_End', N'DeactivateTime', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (20, N'VOD', N'Licensing_Window_Start', N'ActivateTime', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (21, N'VOD', N'ExpiryAfterDownload', N'ExpiryAfterDownload', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (22, N'VOD', N'ExpiryAfterPlay', N'ExpiryAfterPlay', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (23, N'VOD', N'MaxDownloads', N'MaxDownloads', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (24, N'VOD', N'Maximum_Viewing_Length', N'RentalTime', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (25, N'VOD', N'Producer', N'Producer', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (26, N'VOD', N'Rating', N'Rating', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (27, N'VOD', N'Series_ID', N'Series_ID', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (28, N'VOD', N'Series_Name', N'Series_Name', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (29, N'VOD', N'Series_Ordinal', N'Series_Ordinal', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (30, N'VOD', N'Show_ID', N'Show_ID', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (31, N'VOD', N'Show_Name', N'Show_Name', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (32, N'VOD', N'Show_Type', N'ReportingClass', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (33, N'VOD', N'Studio_Code', N'ProviderId', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (34, N'VOD', N'Studio_Name', N'Studio', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (35, N'VOD', N'Suggested_Price', N'ServiceCode', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (36, N'VOD', N'Summary_Long', N'SummaryLong', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (37, N'VOD', N'Summary_Medium', N'SummaryMedium', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (38, N'VOD', N'Summary_Short', N'Description', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (39, N'VOD', N'Title', N'Title', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (40, N'VOD', N'Title_Brief', N'Title', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (41, N'VOD', N'Title_Sort_Name', N'TitleSortName', 1, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (42, N'PTP', N'UI_Location', N'UILocation', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (43, N'VOD', N'Writer', N'Writer', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (44, N'VOD', N'Year', N'YearOfRelease', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1001, N'VOD', N'Run_Time', N'Length', 1, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1002, N'VOD', N'Audio_Type', N'AudioType', 0, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1003, N'VOD', N'CGMS_A', N'AnalogCopy', 0, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1004, N'VOD', N'ExtraData_2', N'CanBeSuspended', 0, 0)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1005, N'VOD', N'HDContent', N'HDContent', 0, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1006, N'VOD', N'Screen_Format', N'ScreenFormat', 0, 1)
INSERT [dbo].[FieldMappings] ([id], [ADI_App_Type], [ADI_Element], [ITV_Element], [IsTitleMetadata], [IsMandatoryField]) VALUES (1007, N'VOD', N'Languages', N'Language', 0, 0)
SET IDENTITY_INSERT [dbo].[FieldMappings] OFF
SET IDENTITY_INSERT [dbo].[MediaLocations] ON 

INSERT [dbo].[MediaLocations] ([id], [MediaLocation], [DeleteFromSource]) VALUES (1, N'D:\itv2adi\media\1', 1)
INSERT [dbo].[MediaLocations] ([id], [MediaLocation], [DeleteFromSource]) VALUES (2, N'D:\itv2adi\media\2', 1)
INSERT [dbo].[MediaLocations] ([id], [MediaLocation], [DeleteFromSource]) VALUES (3, N'D:\itv2adi\media\3', 0)
SET IDENTITY_INSERT [dbo].[MediaLocations] OFF
SET IDENTITY_INSERT [dbo].[ProviderContentTierMapping] ON 

INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (1, N'ODG', N'Vu')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (2, N'Vubiquity', N'Vu')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (3, N'Red Bee Media', N'RBM')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (4, N'RBM', N'RBM')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (5, N'British Broadcasting Corporation', N'BBC')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (6, N'BBC', N'BBC')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (7, N'Digit Studios', N'Digit')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (8, N'CHA', N'CHA')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (9, N'ITV', N'ITV')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (10, N'Filmflex', N'FFX')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (11, N'PPV', N'PPV')
INSERT [dbo].[ProviderContentTierMapping] ([id], [Distributor], [Provider_Content_Tier]) VALUES (13, N'Ericsson', N'ECS')
SET IDENTITY_INSERT [dbo].[ProviderContentTierMapping] OFF
SET IDENTITY_INSERT [dbo].[ReportClassMapping] ON 

INSERT [dbo].[ReportClassMapping] ([id], [Reporting_Class], [ClassIncludes], [Folder_Location], [ShowType]) VALUES (1, N'ReportingClass: CUTV', N'Cartoon,Cartoons,Children,Childrens,Kids,Kids Music,Pre-School,R_Children,RT_Children', N'Kids CUTV', N'Series')
INSERT [dbo].[ReportClassMapping] ([id], [Reporting_Class], [ClassIncludes], [Folder_Location], [ShowType]) VALUES (2, N'ReportingClass: Kids', NULL, N'Kids Archive', N'Series')
INSERT [dbo].[ReportClassMapping] ([id], [Reporting_Class], [ClassIncludes], [Folder_Location], [ShowType]) VALUES (3, N'ReportingClass: TVSVOD', NULL, N'Archive', N'Series')
INSERT [dbo].[ReportClassMapping] ([id], [Reporting_Class], [ClassIncludes], [Folder_Location], [ShowType]) VALUES (4, N'ReportingClass: CUTV', NULL, N'CUTV', N'CUTV')
INSERT [dbo].[ReportClassMapping] ([id], [Reporting_Class], [ClassIncludes], [Folder_Location], [ShowType]) VALUES (5, N'ReportingClass: Movies', NULL, N'Movies', N'Movie')
INSERT [dbo].[ReportClassMapping] ([id], [Reporting_Class], [ClassIncludes], [Folder_Location], [ShowType]) VALUES (6, N'ReportingClass: Adult', NULL, N'Adult', N'Adult')
SET IDENTITY_INSERT [dbo].[ReportClassMapping] OFF
