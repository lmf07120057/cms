﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Abstractions.Dto.Result;
using SS.CMS.Core;
using SS.CMS.Web.Extensions;

namespace SS.CMS.Web.Controllers.V1
{
    [Route("v1/users")]
    public partial class UsersController : ControllerBase
    {
        private const string Route = "";
        private const string RouteActionsLogin = "actions/login";
        private const string RouteActionsLogout = "actions/logout";
        private const string RouteUser = "{id:int}";
        private const string RouteUserAvatar = "{id:int}/avatar";
        private const string RouteUserLogs = "{id:int}/logs";
        private const string RouteUserResetPassword = "{id:int}/actions/resetPassword";

        private readonly IAuthManager _authManager;
        private readonly IConfigRepository _configRepository;
        private readonly IAccessTokenRepository _accessTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserLogRepository _userLogRepository;

        public UsersController(IAuthManager authManager, IConfigRepository configRepository, IAccessTokenRepository accessTokenRepository, IUserRepository userRepository, IUserLogRepository userLogRepository)
        {
            _authManager = authManager;
            _configRepository = configRepository;
            _accessTokenRepository = accessTokenRepository;
            _userRepository = userRepository;
            _userLogRepository = userLogRepository;
        }

        [HttpPost, Route(Route)]
        public async Task<ActionResult<User>> Create([FromBody]User request)
        {
            var user = new User();
            user.LoadDict(request.ToDictionary());

            var config = await _configRepository.GetAsync();

            if (!config.IsUserRegistrationGroup)
            {
                user.GroupId = 0;
            }
            var password = request.Password;

            var valid = await _userRepository.InsertAsync(user, password, string.Empty);
            if (valid.UserId == 0)
            {
                return this.Error(valid.ErrorMessage);
            }

            return await _userRepository.GetByUserIdAsync(valid.UserId);
        }

        [HttpPut, Route(RouteUser)]
        public async Task<ActionResult<User>> Update(int id, [FromBody]User request)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsUserLoggin &&
                         auth.UserId == id ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            var user = await _userRepository.GetByUserIdAsync(id);
            if (user == null) return NotFound();

            var valid = await _userRepository.UpdateAsync(user, request.ToDictionary());
            if (valid.User == null)
            {
                return this.Error(valid.ErrorMessage);
            }

            return valid.User;
        }

        [HttpDelete, Route(RouteUser)]
        public async Task<ActionResult<User>> Delete(int id)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsUserLoggin &&
                         auth.UserId == id ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            auth.UserLogout();
            var user = await _userRepository.DeleteAsync(id);

            return user;
        }

        [HttpGet, Route(RouteUser)]
        public async Task<ActionResult<User>> Get(int id)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsUserLoggin &&
                         auth.UserId == id ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            if (!await _userRepository.IsExistsAsync(id)) return NotFound();

            var user = await _userRepository.GetByUserIdAsync(id);

            return user;
        }

        [HttpGet, Route(RouteUserAvatar)]
        public async Task<StringResult> GetAvatar(int id)
        {
            var user = await _userRepository.GetByUserIdAsync(id);

            var avatarUrl = !string.IsNullOrEmpty(user?.AvatarUrl) ? user.AvatarUrl : _userRepository.DefaultAvatarUrl;
            avatarUrl = PageUtils.AddProtocolToUrl(avatarUrl);

            return new StringResult
            {
                Value = avatarUrl
            };
        }

        [HttpPost, Route(RouteUserAvatar)]
        public async Task<ActionResult<User>> UploadAvatar(int id, [FromBody]UploadAvatarRequest request)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsUserLoggin &&
                         auth.UserId == id ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            var user = await _userRepository.GetByUserIdAsync(id);
            if (user == null) return NotFound();

            if (request.File == null)
            {
                return this.Error("请选择有效的文件上传");
            }

            var fileName = Path.GetFileName(request.File.FileName);

            fileName = _userRepository.GetUserUploadFileName(fileName);
            var filePath = _userRepository.GetUserUploadPath(user.Id, fileName);

            if (!FileUtils.IsImage(PathUtils.GetExtension(fileName)))
            {
                return this.Error("文件只能是 Image 格式，请选择有效的文件上传");
            }

            DirectoryUtils.CreateDirectoryIfNotExists(filePath);
            request.File.CopyTo(new FileStream(filePath, FileMode.Create));

            user.AvatarUrl = _userRepository.GetUserUploadUrl(user.Id, fileName);

            await _userRepository.UpdateAsync(user);

            return user;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<ListResult>> List([FromQuery]ListRequest request)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            var top = request.Top;
            if (top <= 0)
            {
                top = 20;
            }

            var skip = request.Skip;

            var users = await _userRepository.GetUsersAsync(null, 0, 0, null, null, skip, top);
            var count = await _userRepository.GetCountAsync();

            return new ListResult
            {
                Count = count,
                Users = users
            };
        }

        [HttpPost, Route(RouteActionsLogin)]
        public async Task<ActionResult<LoginResult>> Login([FromBody]LoginRequest request)
        {
            var auth = await _authManager.GetApiAsync();

            var valid = await _userRepository.ValidateAsync(request.Account, request.Password, true);
            if (valid.User == null)
            {
                return this.Error(valid.ErrorMessage);
            }

            var accessToken = await auth.UserLoginAsync(valid.UserName, request.IsAutoLogin);
            var expiresAt = DateTime.Now.AddDays(Constants.AccessTokenExpireDays);

            return new LoginResult
            {
                User = valid.User,
                AccessToken = accessToken,
                ExpiresAt = expiresAt
            };
        }

        [HttpPost, Route(RouteActionsLogout)]
        public async Task<User> Logout()
        {
            var auth = await _authManager.GetApiAsync();

            var user = auth.IsUserLoggin ? auth.User : null;
            auth.UserLogout();

            return user;
        }

        [HttpPost, Route(RouteUserLogs)]
        public async Task<ActionResult<UserLog>> CreateLog(int id, [FromBody] UserLog log)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsUserLoggin &&
                         auth.UserId == id ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            var user = await _userRepository.GetByUserIdAsync(id);
            if (user == null) return NotFound();

            var userLog = await _userLogRepository.InsertAsync(user.Id, log);

            return userLog;
        }

        [HttpGet, Route(RouteUserLogs)]
        public async Task<ActionResult<GetLogsResult>> GetLogs(int id, [FromQuery]GetLogsRequest request)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsUserLoggin &&
                         auth.UserId == id ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            var user = await _userRepository.GetByUserIdAsync(id);
            if (user == null) return NotFound();

            var top = request.Top;
            if (top <= 0)
            {
                top = 20;
            }
            var skip = request.Skip;

            var logs = await _userLogRepository.GetLogsAsync(user.Id, skip, top);

            return new GetLogsResult
            {
                Count = await _userRepository.GetCountAsync(),
                Logs = logs
            };
        }

        [HttpPost, Route(RouteUserResetPassword)]
        public async Task<ActionResult<User>> ResetPassword(int id, [FromBody]ResetPasswordRequest request)
        {
            var auth = await _authManager.GetApiAsync();

            var isAuth = auth.IsApiAuthenticated && await
                             _accessTokenRepository.IsScopeAsync(auth.ApiToken, Constants.ScopeUsers) ||
                         auth.IsUserLoggin &&
                         auth.UserId == id ||
                         auth.IsAdminLoggin &&
                         await auth.AdminPermissions.HasSystemPermissionsAsync(Constants.AppPermissions.SettingsUsers);
            if (!isAuth) return Unauthorized();

            var user = await _userRepository.GetByUserIdAsync(id);
            if (user == null) return NotFound();

            if (!_userRepository.CheckPassword(request.Password, false, user.Password, user.PasswordFormat, user.PasswordSalt))
            {
                return this.Error("原密码不正确，请重新输入");
            }

            var valid = await _userRepository.ChangePasswordAsync(user.Id, request.NewPassword);
            if (!valid.IsValid)
            {
                return this.Error(valid.ErrorMessage);
            }

            return user;
        }
    }
}
