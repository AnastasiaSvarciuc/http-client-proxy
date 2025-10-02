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
void RewriteResourceUrls(HtmlNode node)
{
    if (node.NodeType == HtmlNodeType.Element)
    {
        foreach (var attr in new[] { "src", "href" })
        {
            var url = node.GetAttributeValue(attr, null);
            if (!string.IsNullOrEmpty(url))
            {
                // Rewrite absolute URLs to go through proxy
                if (url.StartsWith("http://") || url.StartsWith("https://"))
                    node.SetAttributeValue(attr, "/" + url);
            }
        }
    }
    foreach (var child in node.ChildNodes)
        RewriteResourceUrls(child);
}
void ModifyTextNodes(HtmlNode node)
{
    if (node.NodeType == HtmlNodeType.Text)
    {
        node.InnerHtml = System.Text.RegularExpressions.Regex.Replace(
            node.InnerHtml,
            @"\b\w{6}\b",
            m => m.Value + " TM"
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
