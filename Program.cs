using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PDFApi;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using SelectPdf;
using System.Drawing.Printing;
using System.Text;
using System.Text.Json;

//using PdfReportApi.Documents;
//using QuestPDF.Fluent; // ?? This enables .GeneratePdf()


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient();

var app = builder.Build();

const string apiEndpoint = "https://selectpdf.com/api2/convert/";
const string apiKey = "a36a58f6-b3e2-4bb1-a751-aa94958b4c5d"; // Replace this with your real key

app.MapPost("/convert", async ([FromBody] HtmlRequest request, HttpClient httpClient) =>
{
    if (string.IsNullOrWhiteSpace(request.HtmlContent))
        return Results.BadRequest("HtmlContent cannot be empty.");

    var parameters = new SelectPdfParameters
    {
        key = apiKey,
        html = WrapHtml(request.HtmlContent),
        page_size = "A4",
        page_orientation = "Portrait",
        margins = new Margins { top = 5, right = 5, bottom = 5, left = 5 },  // small margins, avoid zero to prevent blank page
        fit_to_paper = true,
        horizontal_alignment = "Left",
        vertical_alignment = "Top",
        page_breaks_enhanced_algorithm = true,  // Use enhanced page break algorithm to reduce blank pages
        single_page_pdf = false                  // Make sure content is allowed to flow to multiple pages

    };

    var jsonData = JsonConvert.SerializeObject(parameters);
    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

    try
    {
        var response = await httpClient.PostAsync(apiEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            return Results.Problem($"SelectPdf API error: {errorMessage}");
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        return Results.File(pdfBytes, "application/pdf", "converted.pdf");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Exception: {ex.Message}");
    }
});

/// <summary>
/// Wrap raw HTML in full HTML document with minimal styling to avoid blank pages.
/// </summary>
string WrapHtml(string htmlContent)
{
    return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: Arial, sans-serif;
            font-size: 12pt;
        }}
    </style>
</head>
<body>
    {htmlContent}
</body>
</html>";
}
//app.MapGet("/api/report/download", () =>
//{
//    var document = new ClaimReportDocument
//    {
//        ClaimantName = "Nassir Bakkal",
//        PolicyNumber = "A878699908",
//        Email = "nassir.b@xagroup.com",
//        Mobile = "+971 559 6489",
//        LossDate = "22-10-2023",
//        PlaceOfLoss = "Al Quoz, Dubai",
//        BirthDate = "09-06-1989",
//        Address = "22nd Street, Al Quoz 3 Dubai, UAE",
//        Make = "Audi",
//        Model = "A6",
//        Plate = "W-87393",
//        Vin = "4Y1SL65848Z411439"
//    };

//    var pdf = document.GeneratePdf();
//    return Results.File(pdf, "application/pdf", "ClaimReport.pdf");
//});

//app.MapPost("/generate-pdf", ([FromBody] HtmlRequest request) =>
//{
//    if (string.IsNullOrWhiteSpace(request.HtmlContent))
//    {
//        return Results.BadRequest("HTML content is required.");
//    }

//    try
//    {
//        // Optional: Set license key if you have one
//        // SelectPdf.GlobalProperties.LicenseKey = "your-license-key-here";

//        // Create HTML to PDF converter
//        var converter = new HtmlToPdf();
//        converter.Options.MarginBottom = 10;
//        converter.Options.MarginTop = 10;
//        converter.Options.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
//        // Convert HTML string to PDF
//        PdfDocument doc = converter.ConvertHtmlString(request.HtmlContent);

//        // Do not use "using" on the MemoryStream here
//        var ms = new MemoryStream();
//        doc.Save(ms);
//        doc.Close(); // Always close the document

//        ms.Position = 0;

//        // Let ASP.NET Core manage the stream's lifecycle
//        return Results.File(ms, "application/pdf", "generated.pdf");
//    }
//    catch (Exception ex)
//    {
//        return Results.Problem($"PDF generation failed: {ex.Message}");
//    }
//});

//await new BrowserFetcher(new BrowserFetcherOptions
//{
//    Path = ".local-chromium" // optional, controls where Chromium is stored
//}).DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

app.MapPost("/generate-pdf1", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        var payload = System.Text.Json.JsonSerializer.Deserialize<HtmlRequest>(body);
        if (string.IsNullOrWhiteSpace(payload?.HtmlContent))
            return Results.BadRequest("Invalid HTML content.");

        // Inject CSS to control page size and margins for Puppeteer
        string cssPageSettings = @"
            <style>
                @page { size: A4; margin: 10mm; }
                body { margin: 0; padding: 0; }
                .image-block, .section { page-break-inside: avoid; break-inside: avoid; }
            </style>";

        // Insert CSS into HTML head (simple way)
        string htmlWithCss = payload.HtmlContent.Replace(
            "</head>",
            $"{cssPageSettings}</head>"
        );

        // Download Chromium if needed
        await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox" }
        });

        var page = await browser.NewPageAsync();
        await page.SetContentAsync(htmlWithCss);

        var pdfBytes = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "10mm",
                Bottom = "10mm",
                Left = "10mm",
                Right = "10mm"
            },
            PreferCSSPageSize = true
        });

        return Results.File(pdfBytes, "application/pdf", "report.pdf");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

app.Run();
record HtmlPayload(string HtmlContent);
