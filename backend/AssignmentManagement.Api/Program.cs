using System.Text;
using AssignmentManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IRoutineService, RoutineService>();
builder.Services.AddScoped<IAiSchedulingService, AiSchedulingService>();
builder.Services.AddSingleton<IDocumentExtractService, DocumentExtractService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
});
builder.Services.AddSingleton<IOpenAiRoadmapService, OpenAiRoadmapService>();
builder.Services.AddSingleton<IGeminiRoadmapService, GeminiRoadmapService>();
builder.Services.AddSingleton<IAssignmentRoadmapGeminiService, AssignmentRoadmapGeminiService>();
builder.Services.AddSingleton<IAssignmentRoadmapOpenAiService, AssignmentRoadmapOpenAiService>();
builder.Services.AddSingleton<IAssignmentRoadmapOpenRouterService, AssignmentRoadmapOpenRouterService>();
builder.Services.AddSingleton<IAssignmentRoadmapGroqService, AssignmentRoadmapGroqService>();
builder.Services.AddSingleton<IAssignmentAssistantService, AssignmentAssistantService>();
builder.Services.AddSingleton<IAssignmentExtraService, AssignmentExtraService>();
builder.Services.AddScoped<IAssignmentChatService, AssignmentChatService>();
builder.Services.AddScoped<IAssignmentAiResponseService, AssignmentAiResponseService>();
builder.Services.AddScoped<IStressService, StressService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSecretKeyForJwtTokenMinimum32CharactersLong!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AssignmentApi",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AssignmentApp",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
