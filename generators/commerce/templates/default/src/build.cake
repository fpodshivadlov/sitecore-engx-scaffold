// //////////////////////////////////////////////////
// Dependencies
// //////////////////////////////////////////////////

// TODO: temp include - to update to actual version before merge
#tool nuget:https://ci.appveyor.com/nuget/cake-sitecore-cddm77r3s02t?package=Cake.Sitecore&version=0.1.27
#load nuget:https://ci.appveyor.com/nuget/cake-sitecore-cddm77r3s02t?package=Cake.Sitecore&version=0.1.27

// The best practice is to specify the exact version, e.g. "&version=1.0.17"
//#tool nuget:?package=Cake.Sitecore
//#load nuget:?package=Cake.Sitecore

// //////////////////////////////////////////////////
// Arguments
// //////////////////////////////////////////////////

var Target = ArgumentOrEnvironmentVariable("target", "", "Default");

// //////////////////////////////////////////////////
// Prepare
// //////////////////////////////////////////////////

Sitecore.Constants.SetNames();
Sitecore.Parameters.InitParams(
    context: Context,
    msBuildToolVersion: MSBuildToolVersion.Default,
    solutionName: "<%= solutionX %>",
    scSiteUrl: "http://sc9.local", // default URL exposed from the box
    unicornSerializationRoot: "unicorn-<%= solutionUriX %>"
);

Sitecore.Constants.Commerce.SetNames();
Sitecore.Parameters.Commerce.InitParams(
    context: Context
);

// //////////////////////////////////////////////////
// Sitecore Tasks
// //////////////////////////////////////////////////

Task("000-Clean")
    .IsDependentOn(Sitecore.Tasks.ConfigureToolsTaskName)
    .IsDependentOn(Sitecore.Tasks.CleanWildcardFoldersTaskName)
    ;

Task("001-Restore")
    .IsDependentOn(Sitecore.Tasks.RestoreNuGetPackagesTask)
    .IsDependentOn(Sitecore.Tasks.RestoreNpmPackagesTaskName)
    ;

Task("002-Build")
<% if (majorVersion != '9.1') { -%>
    .IsDependentOn(Sitecore.Tasks.GenerateCodeTaskName)
<% } -%>    
    .IsDependentOn(Sitecore.Tasks.BuildClientCodeTaskName)
    .IsDependentOn(Sitecore.Tasks.BuildServerCodeTaskName)
    ;

Task("003-Tests")
    .IsDependentOn(Sitecore.Tasks.RunServerUnitTestsTaskName)
    .IsDependentOn(Sitecore.Tasks.RunClientUnitTestsTaskName)
    ;

Task("004-Packages")
<% if (majorVersion != '9.1') { -%>
    .IsDependentOn(Sitecore.Tasks.CopyShipFilesTaskName)
    .IsDependentOn(Sitecore.Tasks.CopySpeRemotingFilesTaskName)
<% } -%>
    .IsDependentOn(Sitecore.Tasks.RunPackagesInstallationTask)
    ;

Task("005-Publish")
    .IsDependentOn(Sitecore.Tasks.PublishFoundationTaskName)
    .IsDependentOn(Sitecore.Tasks.PublishFeatureTaskName)
    .IsDependentOn(Sitecore.Tasks.PublishProjectTaskName)
    ;

Task("006-Sync-Content")
    .IsDependentOn(Sitecore.Tasks.SyncAllUnicornItems)
    ;

// //////////////////////////////////////////////////
// Commerce Tasks
// //////////////////////////////////////////////////

Task("301-Restore")
    .IsDependentOn(Sitecore.Commerce.Tasks.RestoreNuGetPackagesTask)
    ;

Task("302-Build")
    .IsDependentOn(Sitecore.Commerce.Tasks.BuildCommerceEngineCodeTask)
    ;

Task("303-Tests")
    .IsDependentOn(Sitecore.Commerce.Tasks.RunServerUnitTestsTaskName)
    ;

Task("305-Publish")
    .IsDependentOn(Sitecore.Commerce.Tasks.PublishEngineAuthoringTask)
    .IsDependentOn(Sitecore.Commerce.Tasks.PublishEngineMinionsTask)
    .IsDependentOn(Sitecore.Commerce.Tasks.PublishEngineOpsTask)
    .IsDependentOn(Sitecore.Commerce.Tasks.PublishEngineShopsTask)
    ;

Task("306-Bootstrap")
    .IsDependentOn(Sitecore.Commerce.Tasks.BootstrapCommerceConfigurationTask)
    ;

// //////////////////////////////////////////////////
// Targets
// //////////////////////////////////////////////////

Task("Default-Post")
    .IsDependentOn(Sitecore.Tasks.MergeCoverageReportsTaskName);

Task("Default-Sitecore") // LocalDev
    .IsDependentOn("000-Clean")
    .IsDependentOn("001-Restore")
    .IsDependentOn("002-Build")
    .IsDependentOn("003-Tests")
    .IsDependentOn("004-Packages")
    .IsDependentOn("005-Publish")
    .IsDependentOn("006-Sync-Content");

Task("Default-Commerce") // LocalDev
    .IsDependentOn("301-Restore")
    .IsDependentOn("302-Build")
    .IsDependentOn("303-Tests")
    .IsDependentOn("305-Publish")
    .IsDependentOn("306-Bootstrap");

Task("Default") // LocalDev
    .IsDependentOn("Default-Commerce")
    .IsDependentOn("Default-Sitecore")
    .IsDependentOn("Default-Post");

Task("Sitecore-Build-and-Publish") // LocalDev
    .IsDependentOn("002-Build")
    .IsDependentOn("005-Publish");

Task("Commerce-Build-and-Publish") // LocalDev
    .IsDependentOn("302-Build")
    .IsDependentOn("305-Publish");

// //////////////////////////////////////////////////
// Execution
// //////////////////////////////////////////////////

RunTarget(Target);
