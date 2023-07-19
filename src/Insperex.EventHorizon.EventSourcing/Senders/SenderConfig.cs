﻿using System;
using System.Net;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;

namespace Insperex.EventHorizon.EventSourcing.Senders;

public class SenderConfig
{
    public int BatchSize { get; set; }
    public TimeSpan Timeout { get; set; }
    public Func<Request, HttpStatusCode, string, IResponse> GetErrorResult { get; set; }
}
