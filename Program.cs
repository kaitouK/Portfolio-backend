using System.Reflection.Metadata;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Model;
using MyPortfolio.Service;
using MyPortfolio.Service.Interface;
using MyPortfolio.Repository;
using MyPortfolio.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/myapp-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30) // 每天自動生成新檔案，如 myapp-20260531.txt
    .CreateLogger();
builder.Host.UseSerilog();

string projectRoot = builder.Environment.ContentRootPath;
string keysFolder = Path.Combine(projectRoot, "Keys");

/*
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // 信任 X-Forwarded-For 和 X-Forwarded-Proto 標頭
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    // 如果你的 Nginx/代理伺服器跟你的 API 在同一台機器或網域，通常需要清空已知網路，
    // 讓系統無條件信任前方的 Proxy 傳來的 IP (這在部署個人專案時很常見)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
*/
// 如果資料夾不存在，系統會自動建立
/*builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("MyPortfolioApp"); // 建議固定應用程式名稱*/

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("GlobalPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);  //時間視窗：15分鐘
        opt.PermitLimit = 100;                  //每個IP 允許 100 次請求
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;                     //不排隊，超過直接拒絕
    });
});

builder.Services.AddAuthentication(Options =>
{
    Options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    Options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    //JwtKey需與AuthService相同
    var jwtKey = builder.Configuration["Admin:JwtKey"];
    if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
    {
        throw new InvalidOperationException("【嚴重安全錯誤】JWT Secret Key 遺失或長度不足 32 bytes！請檢查環境變數或 User-Secrets。");
    }
    var key = Encoding.UTF8.GetBytes(jwtKey);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "MyPortfolio",  // 需與 AuthService 簽發時對應
        ValidAudience = "MyPortfolio",
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role // 告訴系統從哪個 Claim 讀取角色資訊
    };

    // 攔截請求，從 Cookie 中提取 JWT Token
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("AppAuth"))
            {
                context.Token = context.Request.Cookies["AppAuth"];
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            // 當 JWT 驗證失敗時，會印出具體原因（例如：金鑰不匹配、Token已過期、Issuer不對）
            Console.WriteLine($"【JWT 驗證失敗原因】: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };

});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClaimTypes.Email, builder.Configuration["Admin:Email"]!);
    });
});
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddScoped<IArtworkService, ArtworkService>();
builder.Services.AddScoped<IArtworkRepository, ArtworkRepository>();
builder.Services.AddScoped<IImageValidator, ImageValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IJournalRepository, JournalRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("https://localhost:5173") // React 開發伺服器的預設 URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();
//如果實際部屬在代理伺服器上要使用下列函式
//app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    // 允許 Google 登入的彈出視窗正常與 React 母網頁通訊
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin-allow-popups");

    // （選配）有時也需要放寬這個標頭
    context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");

    await next();
});
app.UseDefaultFiles(); // 允許服務預設的靜態檔案（如 wwwroot 資料夾中的檔案）
app.UseStaticFiles(); // 允許服務靜態檔案（如上傳的圖片）
app.UseHttpsRedirection();

app.UseRouting();//先路由

app.UseCors("AllowReactApp");//再使用 CORS 中介軟體，確保它在路由之後，這樣才能正確處理跨域請求

app.UseRateLimiter();//啟用rate limiter
app.UseAuthentication(); // 再認證
app.UseAuthorization(); // 最後授權

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    /*using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        //清除journal相關資料表
        dbContext.ClearJournalTables();
    }*/
    app.MapOpenApi();
}

app.MapControllers().RequireRateLimiting("GlobalPolicy");
app.Run();