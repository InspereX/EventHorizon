﻿using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;
using Insperex.EventHorizon.EventStore.ElasticSearch.Attributes;

namespace Insperex.EventHorizon.EventSourcing.Samples.Models.View;

[ViewStore("test_view_search_account")]
[ElasticIndex(Refresh = "true", RefreshIntervalMs = 30000, MaxResultWindow = 5000000)]
public class SearchAccountView : IState
{
    public string Id { get; set; }
    public UserState UserState { get; set; }
    public AccountView Account { get; set; }
}
