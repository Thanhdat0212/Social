using API.ErrorHandling;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Exception Handling & ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Forwarded Headers configuration (chuẩn bị cho 2 tầng proxy: Vercel -> Render)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 4. CORS configuration (tuỳ chọn - mặc định để trống khi dùng proxy cùng origin)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 5. Configure Swagger with JWT Authorize button
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Social API",
        Version = "v1",
        Description = "API mạng xã hội Social — Phase 1 MVP (Auth & Hồ sơ cá nhân)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập Access Token theo định dạng: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ==========================================
// HTTP Request Pipeline (Thứ tự chuẩn mục 7.3)
// ==========================================

// 1. Forwarded Headers
app.UseForwardedHeaders();

// 2. Global Exception Handler
app.UseExceptionHandler();

// 3. Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Social API v1");
    });
}

// 4. HTTPS Redirection
app.UseHttpsRedirection();

// 5. CORS (khi có cấu hình AllowedOrigins)
if (allowedOrigins.Length > 0)
{
    app.UseCors();
}

// 6. Authentication (Đọc JWT từ header)
app.UseAuthentication();

// 7. Authorization (Kiểm tra [Authorize])
app.UseAuthorization();

// 8. Map Controllers
app.MapControllers();

app.Run();
