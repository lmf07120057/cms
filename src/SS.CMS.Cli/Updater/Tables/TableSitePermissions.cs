﻿using System.Collections.Generic;
using Datory;
using Newtonsoft.Json;
using SS.CMS.Abstractions;
using SS.CMS.Core;

namespace SS.CMS.Cli.Updater.Tables
{
    public partial class TableSitePermissions
    {
        private readonly IDatabaseManager _databaseManager;

        public TableSitePermissions(IDatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
        }

        [JsonProperty("roleName")]
        public string RoleName { get; set; }

        [JsonProperty("publishmentSystemID")]
        public long PublishmentSystemId { get; set; }

        [JsonProperty("nodeIDCollection")]
        public string NodeIdCollection { get; set; }

        [JsonProperty("channelPermissions")]
        public string ChannelPermissions { get; set; }

        [JsonProperty("websitePermissions")]
        public string WebsitePermissions { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }
}
