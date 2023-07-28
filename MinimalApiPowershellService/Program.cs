using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.PowerShell;

var options = new WebApplicationOptions
{
    Args = args,
    // Setting to allow the service to run both in IDE and as service
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
};
var builder = WebApplication.CreateBuilder(options);
builder.Host.UseWindowsService();

var app = builder.Build();

app.MapGet("/", () =>
{
    var scriptResponse = new List<string?>();
    const string powerShellScript = @"
param($RequestId = 'DefaultValue')
$env:USERNAME + ""`n"" + $RequestId | Out-File (""c:\Temp\$([DateTime]::Now.ToString(""yyyyMMdd_HHmmss"")).txt"")
Write-Output $RequestId
Write-Output $ENV:UserName
Write-Output $([DateTime]::Now)
";

    var initialSessionState = InitialSessionState.CreateDefault();
    initialSessionState.ExecutionPolicy = ExecutionPolicy.Unrestricted;

    using var powerShell = PowerShell.Create(initialSessionState);
    powerShell.AddScript(powerShellScript);
    powerShell.AddParameter("RequestId", Guid.NewGuid());
    foreach (var line in powerShell.Invoke())
    {
        scriptResponse.Add(line.BaseObject.ToString());
    }
    
    return string.Join("\n", scriptResponse);
});

// RunAsync for service.
await app.RunAsync();