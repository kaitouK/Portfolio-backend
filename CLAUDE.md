# 後端 — ASP.NET Core Web API（.NET 10）

## 分層架構（新功能依此順序加入）

Controller → Service(Interface + 實作) → Repository → Model/Entities

- Controller/ — API 端點，只做接收/驗證/回傳，不放商業邏輯
- Service/ + Service/Interface/ — 商業邏輯
- Repository/ — 資料存取（Repository pattern）
- Model/Entities/ — EF Core 實體
- Migrations/ — EF Core 遷移
- Utility/ — 共用工具
- Keys/ — JWT / Data Protection 金鑰（勿 commit，勿讀寫）
- wwwroot/uploads/journal/ — Journal 上傳圖片

## 指令

- dotnet run # HTTPS，port 7098
- dotnet build
- dotnet ef migrations add <Name> # 改 Entity 後
- dotnet ef database update

## 資料 / 認證

- ORM：EF Core，資料庫：SQLite
- 認證：JWT + Google OAuth
- 改動 Entity 一律搭配 migration，勿手改 SQLite 檔

## 慣例

- 商業邏輯放 Service，不要寫進 Controller
- 對外回傳用 DTO，勿直接暴露 Entity
- CORS 需允許 https://localhost:5173
