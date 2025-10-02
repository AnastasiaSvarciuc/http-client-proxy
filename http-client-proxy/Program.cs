using HtmlAgilityPack;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Map("/{**catchall}", async (HttpContext context) =>
{
    var baseUri = "https://www.reddit.com";
    var path = context.Request.Path.ToString();
    var query = context.Request.QueryString.ToString();
    var targetUrl = baseUri + path + query;
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ConsoleApp3/1.0 (contact: your_email@example.com)");
    var response = await httpClient.GetAsync(targetUrl);
    var content = await response.Content.ReadAsStringAsync();
    var doc = new HtmlDocument();
    doc.LoadHtml(content);
    var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
    if (bodyNode != null) ModifyTextNodes(bodyNode);
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(doc.DocumentNode.OuterHtml);
});

void ModifyTextNodes(HtmlNode node)
{
    var parent = node.ParentNode;
    if (parent != null && (
        parent.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
        parent.Name.Equals("style", StringComparison.OrdinalIgnoreCase) ||
        parent.Name.Equals("noscript", StringComparison.OrdinalIgnoreCase)))
    {
        return;
    }

    if (node.NodeType == HtmlNodeType.Text)
    {
        node.InnerHtml = System.Text.RegularExpressions.Regex.Replace(
            node.InnerHtml,
            @"\b\w{6}\b",
            m => m.Value + "™"
        );
    }
    foreach (var child in node.ChildNodes)
    {
        ModifyTextNodes(child);
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
