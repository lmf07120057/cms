﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Datory;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Abstractions.Dto;
using SS.CMS.Abstractions.Dto.Request;
using SS.CMS.Abstractions.Dto.Result;
using SS.CMS.Core;
using SS.CMS.Core.Serialization;
using SS.CMS.Web.Extensions;

namespace SS.CMS.Web.Controllers.Admin.Cms.Settings
{
    [Route("admin/cms/settings/settingsStyleChannel")]
    public partial class SettingsStyleChannelController : ControllerBase
    {
        private const string Route = "";
        private const string RouteImport = "actions/import";
        private const string RouteExport = "actions/export";

        private readonly IAuthManager _authManager;
        private readonly IPathManager _pathManager;
        private readonly IDatabaseManager _databaseManager;
        private readonly ISiteRepository _siteRepository;
        private readonly IChannelRepository _channelRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ITableStyleRepository _tableStyleRepository;

        public SettingsStyleChannelController(IAuthManager authManager, IPathManager pathManager, IDatabaseManager databaseManager, ISiteRepository siteRepository, IChannelRepository channelRepository, IContentRepository contentRepository, ITableStyleRepository tableStyleRepository)
        {
            _authManager = authManager;
            _pathManager = pathManager;
            _databaseManager = databaseManager;
            _siteRepository = siteRepository;
            _channelRepository = channelRepository;
            _contentRepository = contentRepository;
            _tableStyleRepository = tableStyleRepository;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<GetResult>> Get([FromQuery] ChannelRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSitePermissionsAsync(request.SiteId,
                    Constants.SitePermissions.ConfigTableStyles))
            {
                return Unauthorized();
            }

            var site = await _siteRepository.GetAsync(request.SiteId);
            if (site == null) return NotFound();

            var channel = await _channelRepository.GetAsync(request.ChannelId);

            var styles = new List<Style>();
            foreach (var style in await _tableStyleRepository.GetChannelStyleListAsync(channel))
            {
                styles.Add(new Style
                {
                    Id = style.Id,
                    AttributeName = style.AttributeName,
                    DisplayName = style.DisplayName,
                    InputType = style.InputType.GetDisplayName(),
                    Rules = TranslateUtils.JsonDeserialize<IEnumerable<TableStyleRule>>(style.RuleValues),
                    Taxis = style.Taxis,
                    IsSystem = style.RelatedIdentity != request.ChannelId
                });
            }

            Cascade<int> cascade = null;
            if (request.ChannelId == request.SiteId)
            {
                cascade = await _channelRepository.GetCascadeAsync(site, channel, async summary =>
                {
                    var count = await _contentRepository.GetCountAsync(site, summary);
                    return new
                    {
                        Count = count
                    };
                });
            }

            return new GetResult
            {
                Styles = styles,
                TableName = _channelRepository.TableName,
                RelatedIdentities = _tableStyleRepository.GetRelatedIdentities(channel),
                Channels = cascade
            };
        }

        [HttpDelete, Route(Route)]
        public async Task<ActionResult<ObjectResult<List<Style>>>> Delete([FromBody] DeleteRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSitePermissionsAsync(request.SiteId,
                    Constants.SitePermissions.ConfigTableStyles))
            {
                return Unauthorized();
            }

            var channel = await _channelRepository.GetAsync(request.ChannelId);

            await _tableStyleRepository.DeleteAsync(request.ChannelId, _channelRepository.TableName, request.AttributeName);

            var styles = new List<Style>();
            foreach (var style in await _tableStyleRepository.GetChannelStyleListAsync(channel))
            {
                styles.Add(new Style
                {
                    Id = style.Id,
                    AttributeName = style.AttributeName,
                    DisplayName = style.DisplayName,
                    InputType = style.InputType.GetDisplayName(),
                    Rules = TranslateUtils.JsonDeserialize<IEnumerable<TableStyleRule>>(style.RuleValues),
                    Taxis = style.Taxis,
                    IsSystem = style.RelatedIdentity != request.ChannelId
                });
            }

            return new ObjectResult<List<Style>>
            {
                Value = styles
            };
        }

        [HttpPost, Route(RouteImport)]
        public async Task<ActionResult<BoolResult>> Import([FromBody]ImportRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSitePermissionsAsync(request.SiteId,
                    Constants.SitePermissions.ConfigTableStyles))
            {
                return Unauthorized();
            }

            var channel = await _channelRepository.GetAsync(request.ChannelId);

            if (request.File == null)
            {
                return this.Error("请选择有效的文件上传");
            }

            var fileName = Path.GetFileName(request.File.FileName);

            var sExt = PathUtils.GetExtension(fileName);
            if (!StringUtils.EqualsIgnoreCase(sExt, ".zip"))
            {
                return this.Error("导入文件为 Zip 格式，请选择有效的文件上传");
            }

            var filePath = _pathManager.GetTemporaryFilesPath(fileName);
            DirectoryUtils.CreateDirectoryIfNotExists(filePath);
            request.File.CopyTo(new FileStream(filePath, FileMode.Create));

            var directoryPath = await ImportObject.ImportTableStyleByZipFileAsync(_pathManager, _databaseManager, _channelRepository.TableName, _tableStyleRepository.GetRelatedIdentities(channel), filePath);

            FileUtils.DeleteFileIfExists(filePath);
            DirectoryUtils.DeleteDirectoryIfExists(directoryPath);

            await auth.AddSiteLogAsync(request.SiteId, "导入站点字段");

            return new BoolResult
            {
                Value = true
            };
        }

        [HttpPost, Route(RouteExport)]
        public async Task<ActionResult<StringResult>> Export([FromBody] ChannelRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSitePermissionsAsync(request.SiteId,
                    Constants.SitePermissions.ConfigTableStyles))
            {
                return Unauthorized();
            }

            var channel = await _channelRepository.GetAsync(request.ChannelId);

            var fileName =
                await ExportObject.ExportRootSingleTableStyleAsync(_pathManager, _databaseManager, request.SiteId, _channelRepository.TableName,
                    _tableStyleRepository.GetRelatedIdentities(channel));

            var filePath = _pathManager.GetTemporaryFilesPath(fileName);
            var downloadUrl = PageUtils.GetRootUrlByPhysicalPath(filePath);

            return new StringResult
            {
                Value = downloadUrl
            };
        }
    }
}
