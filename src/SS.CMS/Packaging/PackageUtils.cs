﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Packaging;
using SS.CMS.Abstractions;
using SS.CMS.Core;
using SS.CMS.Plugins;

namespace SS.CMS.Packaging
{
    // https://blog.nuget.org/20130520/Play-with-packages.html
    // https://haacked.com/archive/2011/01/15/building-a-self-updating-site-using-nuget.aspx/
    // https://github.com/caleb-vear/NuSelfUpdate
    public static class PackageUtils
    {
        public const string PackageIdSsCms = "SS.CMS";
        public const string PackageIdSiteServerPlugin = "SS.CMS.Abstractions";
        public const string VersionDev = "0.0.0";

        public const string CacheKeySsCmsIsDownload = nameof(CacheKeySsCmsIsDownload);

        //private const string NuGetPackageSource = "https://packages.nuget.org/api/v2";
        //private const string MyGetPackageSource = "https://www.myget.org/F/siteserver/api/v2";

        //public static bool FindLastPackage(string packageId, out string title, out string version, out DateTimeOffset? published, out string releaseNotes)
        //{
        //    title = string.Empty;
        //    version = string.Empty;
        //    published = null;
        //    releaseNotes = string.Empty;

        //    try
        //    {
        //        var repo =
        //            PackageRepositoryFactory.Default.CreateRepository(WebConfigUtils.AllowNightlyBuild
        //                ? MyGetPackageSource
        //                : NuGetPackageSource);

        //        var package = repo.FindPackage(packageId);

        //        title = package.Title;
        //        version = package.Version.ToString();
        //        published = package.Published;
        //        releaseNotes = package.ReleaseNotes;

        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        // ignored
        //    }

        //    return false;
        //}

        public static void DownloadPackage(string packageId, string version)
        {
            var packagesPath = WebUtils.GetPackagesPath();
            var idWithVersion = $"{packageId}.{version}";
            var directoryPath = PathUtils.Combine(packagesPath, idWithVersion);

            if (DirectoryUtils.IsDirectoryExists(directoryPath))
            {
                if (FileUtils.IsFileExists(PathUtils.Combine(directoryPath, $"{idWithVersion}.nupkg")) && FileUtils.IsFileExists(PathUtils.Combine(directoryPath, $"{packageId}.nuspec")))
                {
                    return;
                }
            }

            var directoryNames = DirectoryUtils.GetDirectoryNames(packagesPath);
            foreach (var directoryName in directoryNames)
            {
                if (StringUtils.StartsWithIgnoreCase(directoryName, $"{packageId}."))
                {
                    DirectoryUtils.DeleteDirectoryIfExists(PathUtils.Combine(packagesPath, directoryName));
                }
            }

            if (StringUtils.EqualsIgnoreCase(packageId, PackageIdSsCms))
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var localFilePath = PathUtils.Combine(directoryPath, idWithVersion + ".nupkg");
                WebClientUtils.SaveRemoteFileToLocal(
                    $"https://api.siteserver.cn/downloads/update/{version}", localFilePath);

                ZipUtils.ExtractZip(localFilePath, directoryPath);
            }
            else
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var localFilePath = PathUtils.Combine(directoryPath, idWithVersion + ".nupkg");

                WebClientUtils.SaveRemoteFileToLocal(
                    $"https://api.siteserver.cn/downloads/package/{packageId}/{version}", localFilePath);

                ZipUtils.ExtractZip(localFilePath, directoryPath);

                //var repo = PackageRepositoryFactory.Default.CreateRepository(WebConfigUtils.IsNightlyUpdate
                //? MyGetPackageSource
                //: NuGetPackageSource);

                //var packageManager = new PackageManager(repo, packagesPath);

                ////Download and unzip the package
                //packageManager.InstallPackage(packageId, SemanticVersion.Parse(version), true, WebConfigUtils.IsNightlyUpdate);
            }

            //ZipUtils.UnpackFilesByExtension(PathUtils.Combine(directoryPath, idWithVersion + ".nupkg"),
            //    directoryPath, ".nuspec");
        }

        public static bool IsPackageDownload(string packageId, string version)
        {
            var packagesPath = WebUtils.GetPackagesPath();
            var idWithVersion = $"{packageId}.{version}";
            var directoryPath = PathUtils.Combine(packagesPath, idWithVersion);

            if (!DirectoryUtils.IsDirectoryExists(directoryPath))
            {
                return false;
            }

            if (!FileUtils.IsFileExists(PathUtils.Combine(directoryPath, $"{idWithVersion}.nupkg")) || !FileUtils.IsFileExists(PathUtils.Combine(directoryPath, $"{packageId}.nuspec")))
            {
                return false;
            }

            if (StringUtils.EqualsIgnoreCase(packageId, PackageIdSsCms))
            {
                var packageWebConfigPath = PathUtils.Combine(directoryPath, Constants.ConfigFileName);
                if (!FileUtils.IsFileExists(packageWebConfigPath))
                {
                    return false;
                }
            }

            return true;
        }

        public static Dictionary<string, string> GetDependencyPackages(PackageMetadata metadata)
        {
            var dict = new Dictionary<string, string>();

            if (metadata != null)
            {
                var dependencyGroups = metadata.GetDependencyGroups();
                foreach (var dependencyGroup in dependencyGroups)
                {
                    foreach (var package in dependencyGroup.Packages)
                    {
                        dict[package.Id] = package.VersionRange.OriginalString;
                    }
                }
            }

            return dict;
        }

        public static bool UpdatePackage(IPathManager pathManager, string idWithVersion, PackageType packageType, out string errorMessage)
        {
            try
            {
                var packagePath = WebUtils.GetPackagesPath(idWithVersion);

                string nuspecPath;
                string dllDirectoryPath;
                var metadata = GetPackageMetadataFromPackages(idWithVersion, out nuspecPath, out dllDirectoryPath, out errorMessage);
                if (metadata == null)
                {
                    return false;
                }

                if (packageType == PackageType.SsCms)
                {
                    var packageWebConfigPath = PathUtils.Combine(packagePath, Constants.ConfigFileName);
                    if (!FileUtils.IsFileExists(packageWebConfigPath))
                    {
                        errorMessage = $"升级包 {Constants.ConfigFileName} 文件不存在";
                        return false;
                    }

                    //DirectoryUtils.Copy(PathUtils.Combine(packagePath, DirectoryUtils.SiteFiles.DirectoryName),
                    //    PathUtils.GetSiteFilesPath(string.Empty), true);
                    //DirectoryUtils.Copy(PathUtils.Combine(packagePath, DirectoryUtils.SiteServer.DirectoryName),
                    //    PathUtils.GetAdminDirectoryPath(string.Empty), true);
                    //DirectoryUtils.Copy(PathUtils.Combine(packagePath, DirectoryUtils.Bin.DirectoryName),
                    //    PathUtils.GetBinDirectoryPath(string.Empty), true);
                    //FileUtils.CopyFile(packageWebConfigPath,
                    //    PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, WebConfigUtils.WebConfigFileName),
                    //    true);
                }
                else if (packageType == PackageType.Plugin)
                {
                    var pluginPath = WebUtils.GetPluginPath(metadata.Id);
                    DirectoryUtils.CreateDirectoryIfNotExists(pluginPath);

                    DirectoryUtils.Copy(PathUtils.Combine(packagePath, "content"), pluginPath, true);
                    DirectoryUtils.Copy(dllDirectoryPath, PathUtils.Combine(pluginPath, "Bin"), true);

                    //var dependencyPackageDict = GetDependencyPackages(metadata);
                    //foreach (var dependencyPackageId in dependencyPackageDict.Keys)
                    //{
                    //    var dependencyPackageVersion = dependencyPackageDict[dependencyPackageId];
                    //    var dependencyDdlDirectoryPath =
                    //        FindDllDirectoryPath(
                    //            PathUtils.GetPackagesPath($"{dependencyPackageId}.{dependencyPackageVersion}"));
                    //    DirectoryUtils.Copy(dependencyDdlDirectoryPath, PathUtils.Combine(pluginPath, "Bin"), true);
                    //}

                    var configFilelPath = PathUtils.Combine(pluginPath, $"{metadata.Id}.nuspec");
                    FileUtils.CopyFile(nuspecPath, configFilelPath, true);

                    PluginManager.ClearCache();
                }
                else if (packageType == PackageType.Library)
                {
                    var fileNames = DirectoryUtils.GetFileNames(dllDirectoryPath);
                    foreach (var fileName in fileNames)
                    {
                        if (StringUtils.EndsWithIgnoreCase(fileName, ".dll"))
                        {
                            var sourceDllPath = PathUtils.Combine(dllDirectoryPath, fileName);
                            var destDllPath = pathManager.GetBinDirectoryPath(fileName);
                            if (!FileUtils.IsFileExists(destDllPath))
                            {
                                FileUtils.CopyFile(sourceDllPath, destDllPath, false);
                            }
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }

            return true;
        }

        public static PackageMetadata GetPackageMetadataFromPluginDirectory(string directoryName, out string errorMessage)
        {
            PackageMetadata metadata = null;

            var nuspecPath = WebUtils.GetPluginNuspecPath(directoryName);
            if (FileUtils.IsFileExists(nuspecPath))
            {
                try
                {
                    metadata = GetPackageMetadata(nuspecPath);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return null;
                }
            }

            if (string.IsNullOrEmpty(metadata?.Id))
            {
                metadata = new PackageMetadata(directoryName);
            }

            errorMessage = string.Empty;
            return metadata;
        }

        public static PackageMetadata GetPackageMetadataFromPackages(string directoryName, out string nuspecPath, out string dllDirectoryPath, out string errorMessage)
        {
            nuspecPath = string.Empty;
            dllDirectoryPath = string.Empty;
            errorMessage = string.Empty;

            var directoryPath = WebUtils.GetPackagesPath(directoryName);

            foreach (var filePath in DirectoryUtils.GetFilePaths(directoryPath))
            {
                if (StringUtils.EqualsIgnoreCase(Path.GetExtension(filePath), ".nuspec"))
                {
                    nuspecPath = filePath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(nuspecPath))
            {
                errorMessage = "配置文件不存在";
                return null;
            }

            PackageMetadata metadata;
            try
            {
                metadata = GetPackageMetadata(nuspecPath);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }

            var packageId = metadata.Id;

            if (string.IsNullOrEmpty(packageId))
            {
                errorMessage = $"配置文件 {nuspecPath} 不正确";
                return null;
            }

            if (!StringUtils.EqualsIgnoreCase(packageId, PackageIdSsCms))
            {
                dllDirectoryPath = FindDllDirectoryPath(directoryPath);

                //if (!FileUtils.IsFileExists(PathUtils.Combine(dllDirectoryPath, packageId + ".dll")))
                //{
                //    errorMessage = $"插件可执行文件 {packageId}.dll 不存在";
                //    return null;
                //}
            }

            return metadata;
        }

        //https://docs.microsoft.com/en-us/nuget/schema/target-frameworks#supported-frameworks
        private static string FindDllDirectoryPath(string packageDirectoryPath)
        {
            var dllDirectoryPath = string.Empty;

            foreach (var dirName in DirectoryUtils.GetDirectoryNames(PathUtils.Combine(packageDirectoryPath, "lib")))
            {
                if (StringUtils.StartsWithIgnoreCase(dirName, "net45") ||
                    StringUtils.StartsWithIgnoreCase(dirName, "net451") ||
                    StringUtils.StartsWithIgnoreCase(dirName, "net452") ||
                    StringUtils.StartsWithIgnoreCase(dirName, "net46") ||
                    StringUtils.StartsWithIgnoreCase(dirName, "net461") ||
                    StringUtils.StartsWithIgnoreCase(dirName, "net462"))
                {
                    dllDirectoryPath = PathUtils.Combine(packageDirectoryPath, "lib", dirName);
                    break;
                }
            }
            if (string.IsNullOrEmpty(dllDirectoryPath))
            {
                dllDirectoryPath = PathUtils.Combine(packageDirectoryPath, "lib");
            }

            return dllDirectoryPath;
        }

        private static PackageMetadata GetPackageMetadata(string configPath)
        {
            var nuspecReader = new NuspecReader(configPath);

            var rawMetadata = nuspecReader.GetMetadata();
            if (rawMetadata == null || !rawMetadata.Any()) return null;

            return PackageMetadata.FromNuspecReader(nuspecReader);
        }

        //**********************************test********************************

        //public static string TestGetLastPackage(bool isPreviewVersion)
        //{
        //    var packageID = "Newtonsoft.Json";

        //    //Connect to the official package repository
        //    IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

        //    IPackage p = repo.FindPackage(packageID);

        //    var builder = new StringBuilder();
        //    builder.Append(p.GetFullName()).Append("<br />");

        //    return builder.ToString();
        //}

        //public static string TestGetReleaseVersionList()
        //{
        //    var packageID = "Newtonsoft.Json";

        //    //Connect to the official package repository
        //    IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

        //    //Get the list of all NuGet packages with ID 'EntityFramework'       
        //    List<IPackage> packages = repo.FindPackagesById(packageID).ToList();

        //    //Filter the list of packages that are not Release (Stable) versions
        //    packages = packages.Where(item => (item.IsReleaseVersion())).ToList();

        //    packages.Reverse();

        //    //Iterate through the list and print the full name of the pre-release packages to console
        //    var builder = new StringBuilder();
        //    foreach (IPackage p in packages)
        //    {
        //        builder.Append(p.GetFullName()).Append("<br />");
        //    }

        //    return builder.ToString();
        //}

        //public static void TestGetAndInstall()
        //{
        //    //ID of the package to be looked up
        //    string packageID = "EntityFramework";

        //    //Connect to the official package repository
        //    IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

        //    //Initialize the package manager
        //    string path = PathUtils.GetPackagesPath();
        //    PackageManager packageManager = new PackageManager(repo, path);

        //    //Download and unzip the package
        //    packageManager.InstallPackage(packageID, SemanticVersion.Parse("5.0.0"));
        //}

        //public static string TestGet10()
        //{
        //    string url = "https://www.nuget.org/api/v2/";
        //    IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(url);
        //    var packages = repo
        //        .GetPackages()
        //        .Where(p => p.IsLatestVersion)
        //        .OrderByDescending(p => p.DownloadCount)
        //        .Take(10);

        //    var builder = new StringBuilder();

        //    foreach (IPackage package in packages)
        //    {
        //        builder.Append(package);
        //    }

        //    return builder.ToString();
        //}

        //public static async Task<string> GetMetadata()
        //{
        //    Logger logger = new Logger();
        //    List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
        //    providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
        //    PackageSource packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
        //    SourceRepository sourceRepository = new SourceRepository(packageSource, providers);

        //    IPackageMetadata

        //    PackageMetadataResource packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
        //    IEnumerable<IPackageSearchMetadata> searchMetadata = await packageMetadataResource.GetMetadataAsync("Wyam.Core", true, true, logger, CancellationToken.None);
        //    var builder = new StringBuilder();
        //    foreach (var packageSearchMetadata in searchMetadata)
        //    {
        //        builder.Append(packageSearchMetadata.);
        //        builder.Append("//////////////////////////////////////////////////////////");
        //    }
        //    return builder.ToString();
        //}
    }

    //public class Logger : ILogger
    //{
    //    public void LogDebug(string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(LogDebug));
    //    }

    //    public void LogVerbose(string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(LogVerbose));
    //    }

    //    public void LogInformation(string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(LogInformation));
    //    }

    //    public void LogMinimal(string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(LogMinimal));
    //    }

    //    public void LogWarning(string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(LogWarning));
    //    }

    //    public void LogError(string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(LogError));
    //    }

    //    public void LogInformationSummary(string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(LogInformationSummary));
    //    }

    //    public void Log(LogLevel level, string data)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(data, nameof(Log));
    //    }

    //    public Task LogAsync(LogLevel level, string data)
    //    {
    //        return null;
    //    }

    //    public void Log(ILogMessage message)
    //    {
    //        GlobalSettings.RecordDao.RecordLog(message.Message, nameof(Log));
    //    }

    //    public Task LogAsync(ILogMessage message)
    //    {
    //        return null;
    //    }
    //}
}
