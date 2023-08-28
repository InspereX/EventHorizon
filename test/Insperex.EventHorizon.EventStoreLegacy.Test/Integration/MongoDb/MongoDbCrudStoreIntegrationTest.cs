using Insperex.EventHorizon.EventStoreLegacy.Test.Integration.Base;
using Insperex.EventHorizon.EventStoreLegacy.Test.Util;
using Xunit;
using Xunit.Abstractions;

namespace Insperex.EventHorizon.EventStoreLegacy.Test.Integration.MongoDb;

[Trait("Category", "Integration")]
public class MongoDbCrudStoreIntegrationTest : BaseCrudStoreIntegrationTest
{
    public MongoDbCrudStoreIntegrationTest(ITestOutputHelper outputHelper) : 
        base(outputHelper, HostTestUtil.GetMongoDbHost(outputHelper).Services)
    {
    }
}