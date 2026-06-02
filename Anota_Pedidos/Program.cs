using Anota_Pedidos.Data;
using Anota_Pedidos.Hubs;
using Anota_Pedidos.Repository;
using Anota_Pedidos.Services;
using Anota_Pedidos.Filters;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURAÇÃO DO RATE LIMITING =====
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 100,         // 100 requisições
            Period = "1m",       // por minuto (para APIs)
        },
        new RateLimitRule
        {
            Endpoint = "POST:/Login/Login",
            Limit = 5,           // 5 tentativas de login
            Period = "5m",       // a cada 5 minutos
        }
    };
    options.StackBlockedRequests = true;
    options.HttpStatusCode = 429; // Too Many Requests
    options.RealIpHeader = "X-Real-IP";
});

builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Adicionar serviço de criptografia
builder.Services.AddScoped<ICryptoService, CryptoService>();

// Serviço de envio de email (para redefinição de senha)
builder.Services.AddScoped<IEmailService, EmailService>();

// Serviço de hash para senhas
builder.Services.AddScoped<IHashService, HashService>();

// Serviço de migração de dados
builder.Services.AddScoped<IEstabelecimentoService, EstabelecimentoService>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers(); // Para API Controllers

// ===== ADICIONAR SERVIÇOS DO SIGNALR =====
builder.Services.AddSignalR();

// ===== ADICIONAR SERVIÇOS DE SESSÃO =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "AlfaPrime.Session";
});

// ===== ADICIONAR HTTP CONTEXT ACCESSOR =====
builder.Services.AddHttpContextAccessor();

// ===== REGISTRAR FILTRO DE AUTENTICAÇÃO =====
builder.Services.AddScoped<AuthFilter>();

// Adicionar HttpClient para o WhatsAppService
builder.Services.AddHttpClient();

// Configurar conexão com MySQL
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

try
{
    using (var connection = new MySqlConnection(connectionString))
    {
        connection.Open();
        Console.WriteLine("✅ Conexão com MySQL estabelecida com sucesso!");
        connection.Close();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro ao conectar ao MySQL: {ex.Message}");
}

// Configurar DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Registrar Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<CategoriaRepository>();
builder.Services.AddScoped<ProdutoRepository>();

// Registrar Services
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// 🔥 CONFIGURAR O RATE LIMITING DEPOIS DO BUILDER (AQUI É O LUGAR CERTO!)
app.UseIpRateLimiting();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ===== USAR SESSÃO ANTES DE AUTHORIZATION =====
app.UseSession();

app.UseAuthorization();

// Mapear o Hub do SignalR
app.MapHub<PedidoHub>("/pedidoHub");

// ===== ROTAS =====

// Rota padrão (tela do usuário)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuario}/{action=Cardapio}/{id?}");

// Rotas para Admin (protegidas)
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Pedidos}/{id?}",
    defaults: new { controller = "Admin" });

// Rota para Estabelecimento
app.MapControllerRoute(
    name: "estabelecimento",
    pattern: "Estabelecimento/{action=Configurar}/{id?}",
    defaults: new { controller = "Estabelecimento" });

// Rota para Login
app.MapControllerRoute(
    name: "login",
    pattern: "Login/{action=Login}/{id?}",
    defaults: new { controller = "Login" });

// Rota para Editar (cardápio)
app.MapControllerRoute(
    name: "editar",
    pattern: "Editar/{action=Editar}/{id?}",
    defaults: new { controller = "Editar" });

app.MapControllerRoute(
    name: "redefinirSenha",
    pattern: "Login/RedefinirSenha",
    defaults: new { controller = "Login", action = "RedefinirSenha" });

// Mapear controllers (para APIs)
app.MapControllers();

app.Run();