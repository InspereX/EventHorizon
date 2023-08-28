using Insperex.EventHorizon.EventStoreLegacy.Test.Integration.Base;
using Insperex.EventHorizon.EventStoreLegacy.Test.Util;
using Xunit;
using Xunit.Abstractions;

namespace Insperex.EventHorizon.EventStoreLegacy.Test.Integration.ElasticSearch;

[Trait("Category", "Integration")]
public class ElasticCrudStoreIntegrationTest : BaseCrudStoreIntegrationTest
{
    public ElasticCrudStoreIntegrationTest(ITestOutputHelper outputHelper) : 
        base(outputHelper, HostTestUtil.GetElasticHost(outputHelper).Services)
    {
    }
}