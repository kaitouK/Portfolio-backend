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
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
   .ReadFrom.Configuration(builder.Configuration)//改成讀取環境變數
    .CreateLogger();
builder.Host.UseSerilog();

string projectRoot = builder.Environment.ContentRootPath;
string keysFolder = Path.Combine(projectRoot, "Keys");


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
Console.WriteLine(
    $"Environment = {builder.Environment.EnvironmentName}"
);
foreach (var item in builder.Configuration.AsEnumerable())
{
    if (item.Key.Contains("Admin"))
    {
        Console.WriteLine($"{item.Key}={item.Value}");
    }
}

builder.Services.AddAuthentication(Options =>
{
    Options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    Options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    //JwtKey需與AuthService相同
    var jwtKey = builder.Configuration.GetSection("Jwt")["Secret"];
    if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
    {
        Log.Fatal("JWT Secret Key 遺失或長度不足 32 bytes！請檢查環境變數或 User-Secrets。");
        throw new InvalidOperationException("JWT Secret Key 遺失或長度不足 32 bytes！請檢查環境變數或 User-Secrets。");
    }
    var key = Encoding.UTF8.GetBytes(jwtKey);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration.GetSection("Jwt")["Issuer"],  // 需與 AuthService 簽發時對應
        ValidAudience = builder.Configuration.GetSection("Jwt")["Audience"],
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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 如果是在 Azure 環境（Linux），確保資料夾存在
if (connectionString.Contains("/home/data/"))
{
    Directory.CreateDirectory("/home/data");
}

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlite(connectionString));

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
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            Log.Fatal("CORS允許的來源網域未設定或為空");
            throw new InvalidOperationException("CORS AllowedOrigins config is missing or empty.");
        }

        policy.WithOrigins(allowedOrigins
        ) // React 開發伺服器的預設 URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear(); // 加上這行，允許來自 Azure 的轉發
forwardedHeadersOptions.KnownProxies.Clear();  // 加上這行，允許來自 Azure 的轉發
forwardedHeadersOptions.ForwardLimit = 1;
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(forwardedHeadersOptions);
}

app.UseDefaultFiles(); // 允許服務預設的靜態檔案（如 wwwroot 資料夾中的檔案）
app.UseStaticFiles(); // 允許服務靜態檔案（如上傳的圖片）
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();//先路由
app.UseSerilogRequestLogging();//濃縮ASP.NET Cor Log日誌

app.UseCors("AllowReactApp");//再使用 CORS 中介軟體，確保它在路由之後，這樣才能正確處理跨域請求

app.UseRateLimiter();//啟用rate limiter
app.UseAuthentication(); // 

app.Use(async (context, next) =>
{
    // 允許 Google 登入的彈出視窗正常與 React 母網頁通訊
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin-allow-popups");

    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";

    context.Response.Headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=()";

    context.Response.Headers["X-Frame-Options"] = "DENY";

    if (context.Request.Path.StartsWithSegments("/api/auth"))
    {
        await next();
        return;
    }
    if (context.User.Identity?.IsAuthenticated == true &&
      (HttpMethods.IsPost(context.Request.Method) ||
       HttpMethods.IsPut(context.Request.Method) ||
       HttpMethods.IsDelete(context.Request.Method) ||
       HttpMethods.IsPatch(context.Request.Method)))
    {

        var origin = context.Request.Headers.Origin.ToString();

        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        {
            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                Log.Fatal("CORS允許的來源網域未設定或為空");
                throw new InvalidOperationException("CORS AllowedOrigins config is missing or empty.");
            }
        }

        if (string.IsNullOrEmpty(origin) ||
            !allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid Origin");
            return;
        }
    }

    await next();
});
app.UseAuthorization(); // 最後授權

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers().RequireRateLimiting("GlobalPolicy");
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    // 這會自動檢查資料庫是否存在，如果不存在就建立並套用最新的 Migration
    dbContext.Database.Migrate();
}
app.Run();