# 後端 — ASP.NET Core Web API（.NET 10）

## 分層架構（新功能依此順序加入）

Controller → Service(Interface + 實作) → Repository → Model/Entities

- Controller/ — API 端點，只做接收/驗證/回傳，不放商業邏輯
- Service/ + Service/Interface/ — 商業邏輯
- Repository/ — 資料存取（Repository pattern）
- Model/Entities/ — EF Core 實體
- Data/ — MyDbContext（資料存取基礎設施）
- DTOs/ — API 輸入/輸出契約，按模組拆檔（ArtworkDtos、JournalDtos…）
- Common/ — 跨層共用型別（ApiResponse、ServiceResult）
- Migrations/ — EF Core 遷移
- Utility/ — 共用工具
- MyPortfolio.Tests/ — xUnit 測試專案（已在主 csproj 用 Compile Remove 排除，勿移除該設定）
- Keys/ — JWT / Data Protection 金鑰（勿 commit，勿讀寫）
- wwwroot/ — 靜態檔案（舊版遺留；上傳圖片現已存放於 Azure Blob）

## 指令

- dotnet run # HTTPS，port 7098
- dotnet build
- dotnet test MyPortfolio.Tests # xUnit 測試（Repository 層用 SQLite in-memory）
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
