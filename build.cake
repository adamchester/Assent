//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"
#addin "MagicChunks"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts";
var projectToPackage = "./src/Assent";

var isContinuousIntegrationBuild = !BuildSystem.IsLocalBuild;

var gitVersionInfo = GitVersion(new GitVersionSettings {
    OutputType = GitVersionOutput.Json
});

var nugetVersion = gitVersionInfo.NuGetVersion;
var cleanups = new List<Action>(); 

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    Information("Building Assent v{0}", nugetVersion);
    if(BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(gitVersionInfo.NuGetVersion);
});

Teardown(context =>
{
    foreach(var item in cleanups)
        item();

    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectories("./src/**/bin");
    CleanDirectories("./src/**/obj");
});

Task("__Restore")
    .Does(() => DotNetCoreRestore());


private void RestoreFileOnCleanup(string file)
{
    var contents = System.IO.File.ReadAllBytes(file);
    cleanups.Add(() => {
        Information("Restoring {0}", file);
        System.IO.File.WriteAllBytes(file, contents);
    });
}


Task("__Build")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Restore")
    //.IsDependentOn("__UpdateProjectJsonVersion")
    .Does(() =>
{
    DotNetCoreBuild("", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("__Test")
    .IsDependentOn("__Build")
    .Does(() =>
{
    GetFiles("**/*Tests/*.csproj")
        .ToList()
        .ForEach(testProjectFile => 
        {
            DotNetCoreTest(testProjectFile.ToString(), new DotNetCoreTestSettings
            {
                Configuration = configuration
            });
        });
});


Task("__Pack")
    .IsDependentOn("__Build")
    .IsDependentOn("__Test")
    .Does(() =>
{
    DotNetCorePack(projectToPackage, new DotNetCorePackSettings
    {
        Configuration = configuration,
        OutputDirectory = artifactsDir,
        NoBuild = true
    });
});

Task("__PushPackages")
    .IsDependentOn("__Pack")
    .Does(() =>
{
    var package = $"{artifactsDir}/Assent.{nugetVersion}.nupkg";
    var localPackagesDir = "../LocalPackages";

    if(DirectoryExists(localPackagesDir))
        CopyFileToDirectory(package, localPackagesDir);

    if(isContinuousIntegrationBuild && gitVersionInfo.PreReleaseTag == "")
    {
        NuGetPush(package, new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey"),
            Timeout = TimeSpan.FromMinutes(20)
        });
    }

});


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("__PushPackages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);