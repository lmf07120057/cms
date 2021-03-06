﻿using Datory;
using Datory.Annotations;

namespace SS.CMS.Abstractions
{
    [DataTable("siteserver_LibraryGroup")]
    public class LibraryGroup : Entity
    {
        [DataColumn]
        public LibraryType Type { get; set; }

        [DataColumn]
        public string GroupName { get; set; }
    }
}
