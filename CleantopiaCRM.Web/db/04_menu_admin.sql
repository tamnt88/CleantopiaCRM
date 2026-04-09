USE CleantopiaCRM;
GO

IF OBJECT_ID('dbo.RoleMenus','U') IS NULL OR OBJECT_ID('dbo.MenuItems','U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng MenuItems/RoleMenus. Hãy chạy 01_schema.sql trước.',16,1);
    RETURN;
END
GO

DELETE FROM dbo.RoleMenus;
DELETE FROM dbo.MenuItems;
GO

INSERT INTO dbo.MenuItems(Code, Title, Url, ParentId, SortOrder, IsActive)
VALUES
('dashboard', N'Tổng quan', '/Dashboard/Index', NULL, 10, 1),
('service', N'Dịch vụ', '/ServicePrices', NULL, 20, 1),
('customers', N'Khách hàng', NULL, NULL, 30, 1),
('customers.list', N'Danh sách khách hàng', '/Customers', (SELECT Id FROM dbo.MenuItems WHERE Code='customers'), 31, 1),
('customers.feedback', N'Feedback', '/Feedbacks', (SELECT Id FROM dbo.MenuItems WHERE Code='customers'), 32, 1),
('schedule', N'Lịch hẹn', '/Appointments', NULL, 40, 1),
('assignment', N'Phân công', '/Assignments', NULL, 50, 1),
('quotes', N'Báo giá', '/Quotes', NULL, 60, 1),
('reminder', N'Bảo trì', '/Reminders', NULL, 70, 1),
('reports', N'Báo cáo', NULL, NULL, 80, 1),
('reports.revenue', N'Doanh thu', '/Reports/Revenue', (SELECT Id FROM dbo.MenuItems WHERE Code='reports'), 81, 1),
('reports.summary', N'Tổng hợp', '/Reports/Summary', (SELECT Id FROM dbo.MenuItems WHERE Code='reports'), 82, 1),
('system', N'Hệ thống', NULL, NULL, 90, 1),
('system.users', N'Người dùng', '/Users', (SELECT Id FROM dbo.MenuItems WHERE Code='system'), 91, 1),
('system.employees', N'Nhân viên', '/Employees', (SELECT Id FROM dbo.MenuItems WHERE Code='system'), 92, 1),
('system.customer-source', N'Nguồn khách', '/CustomerSources', (SELECT Id FROM dbo.MenuItems WHERE Code='system'), 93, 1),
('system.customer-type', N'Loại khách', '/CustomerTypes', (SELECT Id FROM dbo.MenuItems WHERE Code='system'), 94, 1);
GO

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT r.RoleName, m.Id
FROM (VALUES
('Admin'),('DieuPhoi'),('KyThuat'),('GiamSat')
) r(RoleName)
CROSS JOIN dbo.MenuItems m
WHERE r.RoleName = 'Admin';

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT 'DieuPhoi', Id FROM dbo.MenuItems WHERE Code IN
('dashboard','service','customers','customers.list','customers.feedback','schedule','assignment','quotes','reminder','reports','reports.revenue','reports.summary','system.customer-source','system.customer-type');

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT 'KyThuat', Id FROM dbo.MenuItems WHERE Code IN
('dashboard','schedule','assignment','customers','customers.list');

INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
SELECT 'GiamSat', Id FROM dbo.MenuItems WHERE Code IN
('dashboard','customers','customers.list','customers.feedback','schedule','assignment','quotes','reports','reports.revenue','reports.summary');
GO
