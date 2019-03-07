using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

static string webRoot = @"D:\home\site\wwwroot";
static string tempFolder = $@"{webRoot}\temp";

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var format = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "format", true) == 0)
        .Value ?? "CSV";
    var formatSafe = Regex.Replace(format, @"\W", "");

    var method = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "method", true) == 0)
        .Value ?? "CSV";
    var methodSafe = Regex.Replace(format, @"\W", "");

    if (!Directory.Exists(tempFolder))
    {
        Directory.CreateDirectory(tempFolder);
    }

    var filePath = $@"{tempFolder}\{System.Guid.NewGuid().ToString()}.pdf";
    using (var stream = await req.Content.ReadAsStreamAsync())
    {
        var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);
        fileStream.Close();
    }
    
    var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
    var processStartInfo = new ProcessStartInfo
    {
        FileName = javaHome + "\\bin\\java",
        Arguments = $@"-jar tabula.jar --{method.ToLower()} -f {formatSafe.ToUpper()} {filePath}",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        WorkingDirectory = $@"{webRoot}"
    };

    try {
        var process = Process.Start(processStartInfo);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content =  new StringContent(output, Encoding.UTF8, "application/json")
        };
    } catch {
        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
    } finally {
        //File.Delete(filePath);
    }
}
