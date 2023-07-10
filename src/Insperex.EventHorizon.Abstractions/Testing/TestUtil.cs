﻿using System;
using System.Linq;
using Insperex.EventHorizon.Abstractions.Attributes;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Util;

namespace Insperex.EventHorizon.Abstractions.Testing;

public static class TestUtil
{
    public static void SetTestBucketIds(AttributeUtil attributeUtil, params Type[] types)
    {
        var iteration = $"_{Guid.NewGuid().ToString()[..8]}";
        foreach (var type in types)
        {
            var snapAttr = attributeUtil.GetOne<SnapshotStoreAttribute>(type);
            var viewAttr = attributeUtil.GetOne<ViewStoreAttribute>(type);
            var streamAttrs = attributeUtil.GetAll<StreamAttribute>(type);

            if (snapAttr != null) snapAttr.BucketId += iteration;
            if (viewAttr != null) viewAttr.Database += iteration;
            foreach (var streamAttr in streamAttrs)
                streamAttr.Topic += iteration;

            // Update All
            if (snapAttr != null) attributeUtil.Set(type, snapAttr);
            if (viewAttr != null) attributeUtil.Set(type, viewAttr);
            foreach (var streamAttr in streamAttrs)
                attributeUtil.Set(type, streamAttr);
        }
    }
}
