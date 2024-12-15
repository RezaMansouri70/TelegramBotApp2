using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using Telegram.Bot;
using TelegramBotApp.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Disable automatic model state validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddHttpClient("tgwebhook").AddTypedClient<ITelegramBotClient>(
    httpClient => new TelegramBotClient("7657833224:AAE8Vn2ds2jhUg7Xyqst-K_rL-YWnuFNJFk", httpClient));
builder.Services.AddSingleton<UpdateHandler>();
builder.Services.ConfigureTelegramBotMvc();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseDeveloperExceptionPage();


app.Run();
