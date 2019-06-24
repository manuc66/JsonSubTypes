var target = Argument("target", "Default");

Task("Default")
  .Does(() =>
{
   MSBuild("JsonSubTypes.sln");
  Information("Hello World!");
});

RunTarget(target);