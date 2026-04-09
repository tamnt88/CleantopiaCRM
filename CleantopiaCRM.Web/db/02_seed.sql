USE CleantopiaCRM;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE Code='VN')
BEGIN
    INSERT INTO dbo.Countries(Code, Name)
    VALUES ('VN', N'Việt Nam'), ('US', N'Hoa Kỳ'), ('JP', N'Nhật Bản'), ('KR', N'Hàn Quốc');
END
GO

IF OBJECT_ID('dbo.CustomerSources', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.CustomerSources)
BEGIN
    INSERT INTO dbo.CustomerSources(Name, IsActive, SortOrder)
    VALUES
    (N'Trực tiếp', 1, 10),
    (N'Website', 1, 20),
    (N'Facebook', 1, 30),
    (N'Zalo', 1, 40),
    (N'Giới thiệu', 1, 50),
    (N'Telesales', 1, 60);
END
GO

IF OBJECT_ID('dbo.CustomerTypes', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.CustomerTypes)
BEGIN
    INSERT INTO dbo.CustomerTypes(Name, IsBusiness, IsActive, SortOrder)
    VALUES
    (N'Cá nhân', 0, 1, 10),
    (N'Hộ gia đình', 0, 1, 20),
    (N'Doanh nghiệp', 1, 1, 30),
    (N'Đại lý', 1, 1, 40);
END
GO

DELETE FROM dbo.ServicePrices;
GO

INSERT INTO dbo.ServicePrices(Category, ServiceName, VariantName, Unit, Price, Description, IsActive, DisplayOrder)
VALUES
(N'Máy lạnh', N'Vệ sinh máy lạnh', N'Máy thường', N'Lần', 200000, N'Theo bảng giá công khai cleantopia.vn/bang-gia', 1, 1),
(N'Máy lạnh', N'Vệ sinh máy lạnh', N'Máy âm trần', N'Lần', 530000, N'Theo bảng giá công khai cleantopia.vn/bang-gia', 1, 2),
(N'Máy lạnh', N'Bơm Gas R22', N'Máy thường', N'Lần', 250000, NULL, 1, 3),
(N'Máy lạnh', N'Bơm Gas R22', N'Máy âm trần', N'Lần', 600000, NULL, 1, 4),
(N'Máy lạnh', N'Bơm Gas R401', N'Máy thường', N'Lần', 510000, NULL, 1, 5),
(N'Máy lạnh', N'Bơm Gas R401', N'Máy âm trần', N'Lần', 1350000, NULL, 1, 6),
(N'Máy lạnh', N'Bơm Gas R32', N'Máy thường', N'Lần', 510000, NULL, 1, 7),
(N'Máy lạnh', N'Bơm Gas R32', N'Máy âm trần', N'Lần', 1350000, NULL, 1, 8),
(N'Máy lạnh', N'Bơm gas hoàn toàn', N'Máy thường', N'Lần', 1120000, NULL, 1, 9),
(N'Máy lạnh', N'Bơm gas hoàn toàn', N'Âm trần', N'Lần', 2800000, NULL, 1, 10),
(N'Máy lạnh', N'Xử lý rò rỉ, mối nối', N'Máy thường', N'Lần', 340000, NULL, 1, 11),
(N'Máy lạnh', N'Xử lý rò rỉ, mối nối', N'Máy âm trần', N'Lần', 820000, NULL, 1, 12),
(N'Máy lạnh', N'Sửa bo mạch máy inverter', N'Máy thường', N'Lần', 790000, NULL, 1, 13),
(N'Máy lạnh', N'Sửa bo mạch máy inverter', N'Máy âm trần', N'Lần', 1350000, NULL, 1, 14),
(N'Máy lạnh', N'Xử lý chảy nước', N'Máy thường', N'Lần', 340000, NULL, 1, 15),
(N'Máy lạnh', N'Xử lý chảy nước', N'Máy âm trần', N'Lần', 560000, NULL, 1, 16),
(N'Máy lạnh', N'Xử lý xì ga', N'Máy thường', N'Lần', 170000, NULL, 1, 17),
(N'Máy lạnh', N'Xử lý xì ga', N'Máy âm trần', N'Lần', 340000, NULL, 1, 18),
(N'Máy lạnh', N'Xử lý nghẹt ga', N'Máy thường', N'Lần', 170000, NULL, 1, 19),
(N'Máy lạnh', N'Xử lý nghẹt ga', N'Máy âm trần', N'Lần', 340000, NULL, 1, 20);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username='admin')
BEGIN
    INSERT INTO dbo.AppUsers(Username, PasswordHash, FullName, Role, IsActive)
    VALUES ('admin', '240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', N'Quản trị hệ thống', 'Admin', 1);
END
GO
