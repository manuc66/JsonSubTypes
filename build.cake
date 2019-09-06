#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#tool "nuget:?package=OpenCover&version=4.6.519"
#tool nuget:?package=Codecov&version=1.7.1

#addin nuget:?package=Cake.Codecov

var target = Argument("target", "Default");

var repositoryRoot = System.IO.Path.GetFullPath(@"./");
var openCoverDir = System.IO.Path.GetFullPath($"{repositoryRoot}OpenCover");

Task("Default")
  .Does(ctx =>
{
   MSBuild("JsonSubTypes.sln", settings => { 
            settings.SetMaxCpuCount(0);
            settings.WithTarget("Clean");
            settings.SetConfiguration("Release");
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
        });
   MSBuild("JsonSubTypes.sln", settings => { 
            settings.SetMaxCpuCount(0);
            settings.WithTarget("Clean");
            settings.SetConfiguration("Debug");
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
        });
   MSBuild("JsonSubTypes.sln",  settings => { 
            settings.SetMaxCpuCount(0);
            settings.WithTarget("Build");
            settings.SetConfiguration("Release");
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
        });

    var coverSettings = new OpenCoverSettings()
        .WithFilter("+[JsonSubTypes*]*")
        .WithFilter("-[JsonSubTypes.Tests]*");;
         
    coverSettings.ToolPath = ctx.Tools.Resolve("OpenCover.Console.exe");

    coverSettings.SkipAutoProps = true;
    coverSettings.Register = "user";
    coverSettings.MergeByHash = true;
    coverSettings.NoDefaultFilters = true;
    
    CreateDirectory(openCoverDir);

    var openCoverResultFilePath = new FilePath($"{openCoverDir}/_results.xml");
	  var nunitResultFile = new FilePath($"{openCoverDir}/test_results.xml");
    var testAssemblies = GetFiles($@"./**/bin/Release/**/*.Tests.dll");
            
    OpenCover(
        tool => 
        {
            tool.NUnit3(
                testAssemblies, 
                new NUnit3Settings {
                    TeamCity = true,
					Results = new[] { new NUnit3Result { FileName = nunitResultFile}},
                }
            ); 
        },
        openCoverResultFilePath,
        coverSettings);
	
  Codecov(new CodeCovSettings{
    Files =  new string[] {openCoverResultFilePath}
  });
});

RunTarget(target);