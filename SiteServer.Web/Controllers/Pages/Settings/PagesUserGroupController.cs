﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using NSwag.Annotations;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Db;

namespace SiteServer.API.Controllers.Pages.Settings
{
    [OpenApiIgnore]
    [RoutePrefix("pages/settings/userGroup")]
    public class PagesUserGroupController : ApiController
    {
        private const string Route = "";

        [HttpGet, Route(Route)]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                var request = new AuthenticatedRequest();
                if (!request.IsAdminLoggin ||
                    !request.AdminPermissionsImpl.HasSystemPermissions(ConfigManager.SettingsPermissions.User))
                {
                    return Unauthorized();
                }

                var adminNames = (await DataProvider.AdministratorDao.GetUserNameListAsync()).ToList();
                adminNames.Insert(0, string.Empty);

                return Ok(new
                {
                    Value = UserGroupManager.GetUserGroupInfoList(),
                    AdminNames = adminNames
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
                var request = new AuthenticatedRequest();
                if (!request.IsAdminLoggin ||
                    !request.AdminPermissionsImpl.HasSystemPermissions(ConfigManager.SettingsPermissions.User))
                {
                    return Unauthorized();
                }

                var id = request.GetPostInt("id");

                DataProvider.UserGroupDao.Delete(id);

                return Ok(new
                {
                    Value = UserGroupManager.GetUserGroupInfoList()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route(Route)]
        public async Task<IHttpActionResult> Submit([FromBody] UserGroupInfo itemObj)
        {
            try
            {
                var request = new AuthenticatedRequest();
                if (!request.IsAdminLoggin ||
                    !request.AdminPermissionsImpl.HasSystemPermissions(ConfigManager.SettingsPermissions.User))
                {
                    return Unauthorized();
                }

                if (itemObj.Id == -1)
                {
                    if (UserGroupManager.IsExists(itemObj.GroupName))
                    {
                        return BadRequest("保存失败，已存在相同名称的用户组！");
                    }

                    var groupInfo = new UserGroupInfo
                    {
                        GroupName = itemObj.GroupName,
                        AdminName = itemObj.AdminName
                    };

                    DataProvider.UserGroupDao.Insert(groupInfo);

                    await request.AddAdminLogAsync("新增用户组", $"用户组:{groupInfo.GroupName}");
                }
                else if (itemObj.Id == 0)
                {
                    ConfigManager.SystemConfigInfo.UserDefaultGroupAdminName = itemObj.AdminName;

                    DataProvider.ConfigDao.Update(ConfigManager.Instance);

                    UserGroupManager.ClearCache();

                    await request.AddAdminLogAsync("修改用户组", "用户组:默认用户组");
                }
                else if (itemObj.Id > 0)
                {
                    var groupInfo = UserGroupManager.GetUserGroupInfo(itemObj.Id);

                    if (groupInfo.GroupName != itemObj.GroupName && UserGroupManager.IsExists(itemObj.GroupName))
                    {
                        return BadRequest("保存失败，已存在相同名称的用户组！");
                    }

                    groupInfo.GroupName = itemObj.GroupName;
                    groupInfo.AdminName = itemObj.AdminName;

                    DataProvider.UserGroupDao.Update(groupInfo);

                    await request.AddAdminLogAsync("修改用户组", $"用户组:{groupInfo.GroupName}");
                }

                return Ok(new
                {
                    Value = UserGroupManager.GetUserGroupInfoList()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
