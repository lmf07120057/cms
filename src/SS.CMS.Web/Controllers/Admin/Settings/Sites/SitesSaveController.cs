﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Abstractions.Dto.Request;
using SS.CMS.Abstractions.Dto.Result;
using SS.CMS.Core;
using SS.CMS.Core.Serialization;
using SS.CMS.Web.Extensions;

namespace SS.CMS.Web.Controllers.Admin.Settings.Sites
{
    [Route("admin/settings/sitesSave")]
    public partial class SitesSaveController : ControllerBase
    {
        private const string Route = "";
        private const string RouteSettings = "actions/settings";
        private const string RouteFiles = "actions/files";
        private const string RouteActionsData = "actions/data";

        private readonly IAuthManager _authManager;
        private readonly IPathManager _pathManager;
        private readonly IDatabaseManager _databaseManager;
        private readonly ISiteRepository _siteRepository;
        private readonly IChannelRepository _channelRepository;

        public SitesSaveController(IAuthManager authManager, IPathManager pathManager, IDatabaseManager databaseManager, ISiteRepository siteRepository, IChannelRepository channelRepository)
        {
            _authManager = authManager;
            _pathManager = pathManager;
            _databaseManager = databaseManager;
            _siteRepository = siteRepository;
            _channelRepository = channelRepository;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<GetResult>> Get([FromQuery] SiteRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsSites))
            {
                return Unauthorized();
            }

            var site = await _siteRepository.GetAsync(request.SiteId);
            var templateDir = site.Root ? "T_" + site.SiteName : "T_" + site.SiteDir.Replace("\\", "_");

            return new GetResult
            {
                Site = site,
                TemplateDir = templateDir
            };
        }

        [HttpPost, Route(RouteSettings)]
        public async Task<ActionResult<SaveSettingsResult>> SaveSettings([FromBody] SaveRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsSites))
            {
                return Unauthorized();
            }

            var manager = new SiteTemplateManager(_pathManager, _databaseManager);

            if (manager.IsSiteTemplateDirectoryExists(request.TemplateDir))
            {
                return this.Error("站点模板文件夹已存在，请更换站点模板文件夹！");
            }

            var site = await _siteRepository.GetAsync(request.SiteId);

            var directories = new List<string>();
            var files = new List<string>();

            var siteDirList = await _siteRepository.GetSiteDirListAsync(0);
            var fileSystems = FileUtility.GetFileSystemInfoExtendCollection(await _pathManager.GetSitePathAsync(site));
            foreach (FileSystemInfoExtend fileSystem in fileSystems)
            {
                if (!fileSystem.IsDirectory) continue;

                var isSiteDirectory = false;
                if (site.Root)
                {
                    foreach (var siteDir in siteDirList)
                    {
                        if (StringUtils.EqualsIgnoreCase(siteDir, fileSystem.Name))
                        {
                            isSiteDirectory = true;
                        }
                    }
                }
                if (!isSiteDirectory && !WebUtils.IsSystemDirectory(fileSystem.Name))
                {
                    directories.Add(fileSystem.Name);
                }
            }
            foreach (FileSystemInfoExtend fileSystem in fileSystems)
            {
                if (fileSystem.IsDirectory) continue;
                if (!_pathManager.IsSystemFile(fileSystem.Name))
                {
                    files.Add(fileSystem.Name);
                }
            }

            return new SaveSettingsResult
            {
                Directories = directories,
                Files = files
            };
        }

        [HttpPost, Route(RouteFiles)]
        public async Task<ActionResult<SaveFilesResult>> SaveFiles([FromBody]SaveRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsSites))
            {
                return Unauthorized();
            }

            var site = await _siteRepository.GetAsync(request.SiteId);
            var exportObject = new ExportObject(_pathManager, _databaseManager, site);
            var siteTemplatePath = _pathManager.GetSiteTemplatesPath(request.TemplateDir);
            await exportObject.ExportFilesToSiteAsync(siteTemplatePath, request.IsAllFiles, request.CheckedDirectories, request.CheckedFiles, true);

            var channel = await _channelRepository.GetAsync(request.SiteId);
            channel.Children = await _channelRepository.GetChildrenAsync(request.SiteId, request.SiteId);

            return new SaveFilesResult
            {
                Channel = channel
            };
        }

        [HttpPost, Route(RouteActionsData)]
        public async Task<ActionResult<BoolResult>> SaveData([FromBody]SaveRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsSites))
            {
                return Unauthorized();
            }

            var site = await _siteRepository.GetAsync(request.SiteId);

            var siteTemplatePath = _pathManager.GetSiteTemplatesPath(request.TemplateDir);
            var siteContentDirectoryPath = _pathManager.GetSiteTemplateMetadataPath(siteTemplatePath, DirectoryUtils.SiteTemplates.SiteContent);

            var exportObject = new ExportObject(_pathManager, _databaseManager, site);
            await exportObject.ExportSiteContentAsync(siteContentDirectoryPath, request.IsSaveContents, request.IsSaveAllChannels, request.CheckedChannelIds);

            await SiteTemplateManager.ExportSiteToSiteTemplateAsync(_pathManager, _databaseManager, site, request.TemplateDir);

            var siteTemplateInfo = new SiteTemplateInfo
            {
                SiteTemplateName = request.TemplateName,
                PicFileName = string.Empty,
                WebSiteUrl = request.WebSiteUrl,
                Description = request.Description
            };
            var xmlPath = _pathManager.GetSiteTemplateMetadataPath(siteTemplatePath,
                DirectoryUtils.SiteTemplates.FileMetadata);
            Serializer.SaveAsXml(siteTemplateInfo, xmlPath);

            return new BoolResult
            {
                Value = true,
            };
        }
    }
}
