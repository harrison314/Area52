using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using Nuke.Common.Git;


class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository]
    readonly GitRepository Repository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Executes(() =>
        {
            GlobDirectories(SourceDirectory, "**/bin", "**/obj").ForEach(DeleteDirectory);
            GlobDirectories(RootDirectory / "test", "**/bin", "**/obj").ForEach(DeleteDirectory);

            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target BuildArea52 => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            var projectFile = RootDirectory / "src" / "src" / "Area52" / "Area52.csproj";
            var publishPath = RootDirectory / "src" / "src" / "Area52" / "bin" / "publish";

            DotNetBuild(s => s
               .SetProjectFile(projectFile)
               .AddProperty("GitCommit", Repository.Commit)
               .SetConfiguration(Configuration)
           );

            DotNetPublish(s => s
               .SetProject(projectFile)
               .AddProperty("GitCommit", Repository.Commit)
               .SetConfiguration(Configuration)
               .SetOutput(publishPath)
           );

            Nuke.Common.IO.CompressionTasks.CompressZip(publishPath,
                ArtifactsDirectory / "Area52.zip",
                t => t.Extension != ".pdb" && t.Name != "libman.json");

            DeleteDirectory(publishPath);
        });

    Target BuildArea52Tool => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            var projectFile = RootDirectory / "src" / "src" / "Area52.Tool" / "Area52.Tool.csproj";
            var publishPath = RootDirectory / "src" / "src" / "Area52.Tool" / "bin" / "publish";

            DotNetBuild(s => s
               .SetProjectFile(projectFile)
               .AddProperty("GitCommit", Repository.Commit)
               .SetConfiguration(Configuration)
           );

            DotNetPublish(s => s
               .SetProject(projectFile)
               .AddProperty("GitCommit", Repository.Commit)
               .SetConfiguration(Configuration)
               .SetOutput(publishPath)
           );

            Nuke.Common.IO.CompressionTasks.CompressZip(publishPath,
                ArtifactsDirectory / "Area52.Tool.zip");

            DeleteDirectory(publishPath);
        });

    Target BuildArea52Ufo => _ => _
       .DependsOn(Clean)
       .Executes(() =>
       {
           var projectFile = RootDirectory / "src" / "src" / "Area52.Ufo" / "Area52.Ufo.csproj";
           var publishPath = RootDirectory / "src" / "src" / "Area52.Ufo" / "bin" / "publish";

           DotNetBuild(s => s
              .SetProjectFile(projectFile)
              .AddProperty("GitCommit", Repository.Commit)
              .SetConfiguration(Configuration)
          );

           DotNetPublish(s => s
              .SetProject(projectFile)
              .AddProperty("GitCommit", Repository.Commit)
              .SetConfiguration(Configuration)
              .SetOutput(publishPath)
          );

           Nuke.Common.IO.CompressionTasks.CompressZip(publishPath,
               ArtifactsDirectory / "Area52.Ufo.zip");

           DeleteDirectory(publishPath);
       });

    Target Compile => _ => _
        .DependsOn(BuildArea52)
        .DependsOn(BuildArea52Ufo)
        .DependsOn(BuildArea52Tool)
        .Executes(() =>
        {
            
        });
}
