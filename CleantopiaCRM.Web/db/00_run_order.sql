-- Run order:
-- 1) 01_schema.sql (new full setup)
-- 2) 02_seed.sql
-- 3) 03_seed_countries_vi.sql (full quốc gia tiếng Việt)
-- 4) 04_menu_admin.sql (menu admin theo role trong database)
-- For existing DB only (không chạy lại 01_schema):
-- A) 04a_upgrade_menu_schema.sql
-- B) 04_menu_admin.sql
-- Then start app and go to /AddressAdmin/SyncGhn to pull latest GHN provinces/wards
-- GHN source based on document: "GHN - Cập nhật API - Đáp ứng đơn vị hành chính mới.docx"

-- 6) 05_menu_grouping_icons.sql (gom menu cha/con + icon)

-- 7) 06_dashboard_data_upgrade.sql (bo sung du lieu dashboard 360)
-- 8) 07_customer_master_upgrade.sql (nguon khach, loai khach, nhieu dia chi dich vu, hoa don doanh nghiep)
-- 9) 08_service_price_policy_upgrade.sql (tach don vi + chu ky tai cham soc cho bang gia dich vu)
-- 10) 09_service_categories_parent_child.sql (quan ly danh muc dich vu cha/con thuần)
-- 11) 10_quote_discount_upgrade.sql (bo sung giam gia bao gia + giam gia/ghi chu tung dong)
-- 12) 11_quote_address_upgrade.sql (bo sung dia chi va nguoi lien he cho bao gia)
-- 13) 12_customer_code_upgrade.sql (bo sung ma khach hang CustomerCode)
-- 14) 13_quote_vat_rate_upgrade.sql (bo sung VAT cho bao gia)
-- 15) 14_quote_full_info_upgrade.sql (bo sung thong tin dia chi, hoa don, tong tien cho bao gia)

