﻿namespace Insperex.EventHorizon.EventStore.ElasticSearchLegacy.Models;

public class ElasticConfig
{
    public string[] Uris { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}