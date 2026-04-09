USE CleantopiaCRM;
GO

IF OBJECT_ID('dbo.MenuItems','U') IS NULL OR OBJECT_ID('dbo.RoleMenus','U') IS NULL
BEGIN
    RAISERROR(N'Chưa có MenuItems/RoleMenus. Chạy 04a_upgrade_menu_schema.sql trước.',16,1);
    RETURN;
END
GO

IF OBJECT_ID('tempdb..#MenuSeed') IS NOT NULL DROP TABLE #MenuSeed;
CREATE TABLE #MenuSeed
(
    Code NVARCHAR(100) NOT NULL,
    Title NVARCHAR(250) NOT NULL,
    Url NVARCHAR(300) NULL,
    IconCss NVARCHAR(100) NULL,
    ParentCode NVARCHAR(100) NULL,
    SortOrder INT NOT NULL,
    IsActive BIT NOT NULL
);

INSERT INTO #MenuSeed(Code, Title, Url, IconCss, ParentCode, SortOrder, IsActive)
VALUES
('dashboard', N'Tổng quan', '/Dashboard/Index', 'fa-solid fa-chart-line', NULL, 10, 1),

('customer', N'Khách hàng', NULL, 'fi-rr-users', NULL, 20, 1),
('customer.list', N'Danh sách khách hàng', '/Customers', 'fa-solid fa-address-book', 'customer', 21, 1),
('customer.feedback', N'Feedback', '/Feedbacks', 'fi-rr-comment-alt', 'customer', 22, 1),

('service', N'Dịch vụ', '/ServicePrices', 'fa-solid fa-screwdriver-wrench', NULL, 30, 1),

('operation', N'Vận hành', NULL, 'fa-regular fa-calendar-days', NULL, 40, 1),
('operation.appointment', N'Lịch hẹn', '/Appointments', 'fa-regular fa-calendar-check', 'operation', 41, 1),
('operation.assignment', N'Phân công', '/Assignments', 'fa-solid fa-user-check', 'operation', 42, 1),
('operation.quote', N'Báo giá', '/Quotes', 'fa-solid fa-file-invoice-dollar', 'operation', 43, 1),
('operation.reminder', N'Bảo trì', '/Reminders', 'fi-rr-alarm-clock', 'operation', 44, 1),

('report', N'Báo cáo', NULL, 'fi-rr-chart-line-up', NULL, 50, 1),
('report.revenue', N'Doanh thu', '/Reports/Revenue', 'fa-solid fa-sack-dollar', 'report', 51, 1),
('report.summary', N'Tổng hợp', '/Reports/Summary', 'fi-rr-analyse', 'report', 52, 1),

('system', N'Hệ thống', NULL, 'fa-solid fa-gear', NULL, 60, 1),
('system.user', N'Người dùng', '/Users', 'fa-solid fa-user-gear', 'system', 61, 1),
('system.employee', N'Nhân viên', '/Employees', 'fi-rr-id-badge', 'system', 62, 1),
('system.customer-source', N'Nguồn khách', '/CustomerSources', 'fa-solid fa-bullhorn', 'system', 63, 1),
('system.customer-type', N'Loại khách', '/CustomerTypes', 'fa-solid fa-tags', 'system', 64, 1);

-- Upsert root first
MERGE dbo.MenuItems AS t
USING (
    SELECT Code, Title, Url, IconCss, SortOrder, IsActive
    FROM #MenuSeed WHERE ParentCode IS NULL
) AS s
ON t.Code = s.Code
WHEN MATCHED THEN
    UPDATE SET t.Title=s.Title, t.Url=s.Url, t.IconCss=s.IconCss, t.SortOrder=s.SortOrder, t.IsActive=s.IsActive
WHEN NOT MATCHED THEN
    INSERT(Code, Title, Url, IconCss, ParentId, SortOrder, IsActive)
    VALUES(s.Code, s.Title, s.Url, s.IconCss, NULL, s.SortOrder, s.IsActive);

-- Upsert children with resolved parent
MERGE dbo.MenuItems AS t
USING (
    SELECT c.Code, c.Title, c.Url, c.IconCss, p.Id AS ParentId, c.SortOrder, c.IsActive
    FROM #MenuSeed c
    JOIN dbo.MenuItems p ON p.Code = c.ParentCode
    WHERE c.ParentCode IS NOT NULL
) AS s
ON t.Code = s.Code
WHEN MATCHED THEN
    UPDATE SET t.Title=s.Title, t.Url=s.Url, t.IconCss=s.IconCss, t.ParentId=s.ParentId, t.SortOrder=s.SortOrder, t.IsActive=s.IsActive
WHEN NOT MATCHED THEN
    INSERT(Code, Title, Url, IconCss, ParentId, SortOrder, IsActive)
    VALUES(s.Code, s.Title, s.Url, s.IconCss, s.ParentId, s.SortOrder, s.IsActive);

-- Keep only seed menu active, others keep untouched (no delete)

-- Reset role mapping for core roles
DELETE FROM dbo.RoleMenus WHERE RoleName IN ('Admin','DieuPhoi','KyThuat','GiamSat');

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT 'Admin', Id FROM dbo.MenuItems WHERE Code IN (
'dashboard','customer','customer.list','customer.feedback','service','operation','operation.appointment','operation.assignment','operation.quote','operation.reminder','report','report.revenue','report.summary','system','system.user','system.employee','system.customer-source','system.customer-type'
);

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT 'DieuPhoi', Id FROM dbo.MenuItems WHERE Code IN (
'dashboard','customer','customer.list','customer.feedback','service','operation','operation.appointment','operation.assignment','operation.quote','operation.reminder','report','report.revenue','report.summary','system.customer-source','system.customer-type'
);

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT 'KyThuat', Id FROM dbo.MenuItems WHERE Code IN (
'dashboard','operation','operation.appointment','operation.assignment','customer','customer.list'
);

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT 'GiamSat', Id FROM dbo.MenuItems WHERE Code IN (
'dashboard','customer','customer.list','customer.feedback','operation','operation.appointment','operation.assignment','operation.quote','report','report.revenue','report.summary'
);

DROP TABLE #MenuSeed;
GO
