using System.Diagnostics;
using System.Text.Json;
using System.Data.SqlClient;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

// Select the right one path for NTAC-IDP

Console.WriteLine("WELCOME TO IDP CONFIGURATOR\n");
Console.WriteLine("Please insert path to your project solution: \n\nExample: C:\\Users\\Korisnik\\source\\repos\\ConsoleApp1");
Console.WriteLine("Info: The absolute path must be to solution.sln file");

var projectPath = Console.ReadLine();
var pathRegex = @"^[a-zA-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*$";

projectPath = IsEmptyOrNullPath(projectPath);

var isValidPath = Regex.IsMatch(projectPath, pathRegex);

IsValidPath(ref projectPath, pathRegex, ref isValidPath);

string fileName = "NTAC-IDP.sln";
string filePath = Path.Combine(projectPath, fileName);

while (!File.Exists(filePath))
{
    Console.WriteLine("The file does not exist. Please enter correct path!");
    projectPath = Console.ReadLine();
    projectPath = IsEmptyOrNullPath(projectPath);

    isValidPath = Regex.IsMatch(projectPath, pathRegex);

    IsValidPath(ref projectPath, pathRegex, ref isValidPath);
    filePath = Path.Combine(projectPath, fileName);
    if (File.Exists(filePath)) break;
}

Console.WriteLine("File exist!");

// Internet connection testing

Console.WriteLine("Internet connection testing...");
Thread.Sleep(2000);
string command = "ping";
string arguments = "-n 1 www.google.com";

ProcessStartInfo psiConnection = new ProcessStartInfo(command, arguments);
psiConnection.RedirectStandardOutput = true;
psiConnection.UseShellExecute = false;

Process processConnection = new Process();
processConnection.StartInfo = psiConnection;
processConnection.Start();

string result = processConnection.StandardOutput.ReadToEnd();
Console.WriteLine(result);

processConnection.WaitForExit();
int exitCode = processConnection.ExitCode;
Console.WriteLine(exitCode);
if (exitCode == 1)
{
    Console.WriteLine("Please check your internet connection!");
    Environment.Exit(exitCode);
}
Console.WriteLine("Successfully connected!");
Thread.Sleep(2000);

// Install .Net Core 3.1 Runtime

Console.WriteLine("Installing the .Net Core 3.1 runtime...");
Console.WriteLine("Please Wait...");
Thread.Sleep(2000);

string currentDir = Directory.GetCurrentDirectory();
for (int i = 0; i < 3; i++)
{
    currentDir = Directory.GetParent(currentDir).FullName;
}
string installerPath = Path.Combine(currentDir, "windowsdesktop-runtime-3.1.32-win-x64.exe");
string argumentsForInstaller = "/q";
ProcessStartInfo psiRuntime = new ProcessStartInfo(installerPath, argumentsForInstaller);

Process installerProcess = new Process();
installerProcess.StartInfo = psiRuntime;
installerProcess.Start();

installerProcess.WaitForExit();

int installerExitCode = installerProcess.ExitCode;
if (installerExitCode == 0)
{
    Console.WriteLine(".Net Core 3.1 runtime installed successfully.");
}
else
{
    Console.WriteLine(".Net Core 3.1 runtime installation failed with exit code.", installerExitCode);
}

// Get application url 

string launchSettingsPath = projectPath + "\\NTAC-IDP\\Properties\\launchSettings.json";
string launchSettingsContents = File.ReadAllText(launchSettingsPath);

var jsonObject = JsonObject.Parse(launchSettingsContents);
var applicationUrl = jsonObject["profiles"]?["NTAC-IDP"]?["applicationUrl"];

string[] partsUrl = applicationUrl.ToString().Split(";");
string httpsAppUrl = partsUrl[0];

Console.WriteLine("Create appsettings.Development.json file on local machine...");
Thread.Sleep(2000);

// Setup appsettings develppment file

Console.WriteLine("Please enter full Sql Server name on your local machine?\nExample: 'DESKTOP-AP9BKUN\\NEWSERVER'");
var sqlServerInstance = Console.ReadLine();
Console.WriteLine("Your database name is 'NTACIDP_Test' because Logical Name and Pysical Name (meta information)");
Thread.Sleep(2000);

Console.WriteLine("Please enter Debug Email Receiver:");
var debugEmail = Console.ReadLine();

var sqlDatabaseSchema = "NTACIDP_Test";
var connectionString = $"Server={sqlServerInstance};Initial Catalog={sqlDatabaseSchema};Integrated Security=True;MultipleActiveResultSets=False;Connection Timeout=5";
string jsonFullPath = currentDir + "\\appsettings.Development.json";

string jsonFile = File.ReadAllText(jsonFullPath);
var jsonData = JsonObject.Parse(jsonFile);

var mssqlServer = jsonData["Serilog"]?["WriteTo"]?[1];
mssqlServer["Args"]["connectionString"] = connectionString;
jsonData["ConnectionStrings"]["NtacIdp"] = connectionString;
jsonData["DebugEmailReceiver"] = debugEmail;
jsonData["Domains"]["IDP"] = httpsAppUrl;

Console.WriteLine("Setup file in process...");
Thread.Sleep(2000);
Console.WriteLine("Create configured file to destination...");
Thread.Sleep(2000);

// Create configured file to project path

string filePathAppSettings = projectPath + "\\NTAC-IDP\\appsettings.Development.json";

if (!File.Exists(filePathAppSettings))
{
    using (StreamWriter writer = File.CreateText(filePathAppSettings))
    {
        writer.Write(jsonData.ToString());
    }
    Console.WriteLine("File created successfully at {0}", filePathAppSettings);
}
else
{
    File.WriteAllText(filePathAppSettings, string.Empty);
    File.WriteAllText(filePathAppSettings, jsonData.ToString());

    Console.WriteLine("File already exists and content is replaced at {0}\n", filePathAppSettings);
}

// Import database from absolute path

Console.WriteLine("Connection is work if you have windows authentication and Encrypt=False(connection does not have to be encrypted)");
Console.WriteLine("Press key to continue...\n");
Console.Read();

Console.WriteLine("Enter absolute path to backup file?\nExample: C:\\NTACIDP_Test.bak");
var databaseBackupPath = Console.ReadLine();
string connectionStr = $"Server={sqlServerInstance};Database=master;Trusted_Connection=True;Encrypt=False;";
string backupFilePath = @"C:\NTACIDP_Test.bak";

SqlConnection sqlConnection = new SqlConnection(connectionStr);
sqlConnection.InfoMessage += new SqlInfoMessageEventHandler(ConnectionInfoMessage);

ServerConnection serverConnection = new ServerConnection(sqlConnection);
Server server = new Server(serverConnection);

Restore restore = new Restore();
restore.Database = sqlDatabaseSchema;
restore.Action = RestoreActionType.Database;
restore.ReplaceDatabase = true;
restore.Devices.AddDevice(backupFilePath, DeviceType.File);
restore.SqlRestore(server);

static void ConnectionInfoMessage(object sender, SqlInfoMessageEventArgs e)
{
    Console.WriteLine(e.Message);
}


Console.Read();






static string IsEmptyOrNullPath(string? projectPath)
{
    while (string.IsNullOrEmpty(projectPath))
    {
        Console.WriteLine("Please enter path!");
        projectPath = Console.ReadLine();
    }

    return projectPath;
}

static void IsValidPath(ref string? projectPath, string pathRegex, ref bool isValidPath)
{
    while (!isValidPath)
    {
        projectPath = IsEmptyOrNullPath(projectPath);
        isValidPath = Regex.IsMatch(projectPath, pathRegex);
        if (isValidPath) break;

        Console.WriteLine("Please enter valid path!");
       
        projectPath = Console.ReadLine();
        isValidPath = Regex.IsMatch(projectPath, pathRegex);

        if (isValidPath) break;
    }
}