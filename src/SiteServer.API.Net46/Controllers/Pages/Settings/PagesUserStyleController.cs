﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using SiteServer.API.Common;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Model.Attributes;
using SiteServer.Utils;

namespace SiteServer.API.Controllers.Pages.Settings
{
    [RoutePrefix("pages/settings/userStyle")]
    public class PagesUserStyleController : ControllerBase
    {
        private const string Route = "";

        [HttpGet, Route(Route)]
        public IHttpActionResult Get()
        {
            try
            {
                var request = GetRequest();
                if (!request.IsAdminLoggin ||
                    !request.AdminPermissionsImpl.HasSystemPermissions(ConfigManager.SettingsPermissions.User))
                {
                    return Unauthorized();
                }

                var list = new List<object>();
                foreach (var styleInfo in TableStyleManager.GetUserStyleInfoList())
                {
                    list.Add(new
                    {
                        styleInfo.Id,
                        styleInfo.AttributeName,
                        styleInfo.DisplayName,
                        InputType = InputTypeUtils.GetText(styleInfo.Type),
                        Validate = styleInfo.VeeValidate,
                        styleInfo.Taxis,
                        IsSystem = StringUtils.ContainsIgnoreCase(UserAttribute.AllAttributes.Value, styleInfo.AttributeName)
                    });
                }

                return Ok(new
                {
                    Value = list,
                    DataProvider.UserDao.TableName,
                    RelatedIdentities = TableStyleManager.EmptyRelatedIdentities
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete, Route(Route)]
        public IHttpActionResult Delete()
        {
            try
            {
                var request = GetRequest();
                if (!request.IsAdminLoggin ||
                    !request.AdminPermissionsImpl.HasSystemPermissions(ConfigManager.SettingsPermissions.Admin))
                {
                    return Unauthorized();
                }

                var attributeName = request.GetPostString("attributeName");

                DataProvider.TableStyleDao.Delete(0, DataProvider.UserDao.TableName, attributeName);

                var list = new List<object>();
                foreach (var styleInfo in TableStyleManager.GetUserStyleInfoList())
                {
                    list.Add(new
                    {
                        styleInfo.Id,
                        styleInfo.AttributeName,
                        styleInfo.DisplayName,
                        InputType = InputTypeUtils.GetText(styleInfo.Type),
                        Validate = TableStyleManager.GetValidateInfo(styleInfo),
                        styleInfo.Taxis,
                        IsSystem = StringUtils.ContainsIgnoreCase(UserAttribute.AllAttributes.Value, styleInfo.AttributeName)
                    });
                }

                return Ok(new
                {
                    Value = list
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
