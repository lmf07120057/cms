using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using SS.CMS.Abstractions;
using SS.CMS.Plugins;

namespace SS.CMS.Core
{
    public class TabManager
    {
        private readonly IPathManager _pathManager;
        public TabManager(IPathManager pathManager)
        {
            _pathManager = pathManager;
        }

	    public TabCollection GetTabs(string filePath)
	    {
	        var tc = CacheUtils.Get(filePath) as TabCollection;
	        if (tc != null) return tc;

	        tc = Serializer.ConvertFileToObject<TabCollection>(filePath);
	        CacheUtils.Insert(filePath, tc, filePath);
	        return tc;
	    }

        public string GetMenusPath(params string[] paths)
        {
            return PathUtils.Combine(_pathManager.GetAdminPath("assets/menus"), PathUtils.Combine(paths));
        }

        public List<Tab> GetTopMenuTabs()
        {
            var list = new List<Tab>();

            var menuPath = GetMenusPath("Top.config");
            if (!FileUtils.IsFileExists(menuPath)) return list;

            var tabs = GetTabs(menuPath);
            foreach (var parent in tabs.Tabs)
            {
                list.Add(parent);
            }

            return list;
        }

	    public List<Tab> GetTopMenuTabsWithChildren()
	    {
	        var list = new List<Tab>();

	        var menuPath = GetMenusPath("Top.config");
	        if (!FileUtils.IsFileExists(menuPath)) return list;

	        var tabs = GetTabs(menuPath);
	        foreach (var parent in tabs.Tabs)
	        {
	            if (parent.HasChildren)
	            {

	            }
	            list.Add(parent);
	        }

	        return list;
	    }

        public bool IsValid(Tab tab, IList permissionList)
        {
            if (tab.HasPermissions)
            {
                if (permissionList != null && permissionList.Count > 0)
                {
                    var tabPermissions = tab.Permissions.Split(',');
                    foreach (var tabPermission in tabPermissions)
                    {
                        if (permissionList.Contains(tabPermission))
                            return true;
                    }
                }

                //ITab valid, but invalid role set
                return false;
            }

            //ITab valid, but no roles
            return true;
        }

        public async Task<List<Tab>> GetTabListAsync(string topId, int siteId)
        {
            var tabs = new List<Tab>();

            if (!string.IsNullOrEmpty(topId))
            {
                var filePath = GetMenusPath($"{topId}.config");
                var tabCollection = GetTabs(filePath);
                if (tabCollection?.Tabs != null)
                {
                    foreach (var tabCollectionTab in tabCollection.Tabs)
                    {
                        tabs.Add(tabCollectionTab.Clone());
                    }
                }
            }

            var menus = new List<Menu>();
            if (siteId > 0 && topId == string.Empty)
            {
                var siteMenus = await PluginMenuManager.GetSiteMenusAsync(siteId);
                if (siteMenus != null)
                {
                    menus.AddRange(siteMenus);
                }
            }
            else if (topId == "Plugins")
            {
                var topMenus = await PluginMenuManager.GetTopMenusAsync();
                if (topMenus != null)
                {
                    menus.AddRange(topMenus);
                }
            }

            foreach (var menu in menus)
            {
                var isExists = false;
                foreach (var childTab in tabs)
                {
                    if (childTab.Id == menu.Id)
                    {
                        isExists = true;
                    }
                }

                if (isExists) continue;

                tabs.Add(PluginMenuManager.GetPluginTab(menu.PluginId, string.Empty, menu));

                //if (string.IsNullOrEmpty(menu.ParentId))
                //{
                //    var isExists = false;
                //    foreach (var childTab in tabs)
                //    {
                //        if (childTab.Id == menu.Id)
                //        {
                //            isExists = true;
                //        }
                //    }

                //    if (isExists) continue;

                //    tabs.Add(GetPluginTab(menu));
                //}
                //else
                //{
                //    foreach (var tab in tabs)
                //    {
                //        if (!StringUtils.EqualsIgnoreCase(menu.ParentId, tab.Id)) continue;

                //        var isExists = false;
                //        foreach (var childTab in tab.Children)
                //        {
                //            if (childTab.Id == menu.Id)
                //            {
                //                isExists = true;
                //            }
                //        }

                //        if (isExists) continue;

                //        var list = new List<Tab>();
                //        if (tab.Children != null)
                //        {
                //            list = tab.Children.ToList();
                //        }
                //        list.Add(GetPluginTab(menu));
                //        tab.Children = list.ToArray();
                //    }
                //}
            }

            return tabs;
        }
    }
}
