using Insperex.EventHorizon.EventStoreLegacy.Test.Integration.Base;
using Insperex.EventHorizon.EventStoreLegacy.Test.Util;
using Xunit;
using Xunit.Abstractions;

namespace Insperex.EventHorizon.EventStoreLegacy.Test.Integration.InMemory;

[Trait("Category", "Integration")]
public class InMemoryCrudStoreIntegrationTest : BaseCrudStoreIntegrationTest
{
    public InMemoryCrudStoreIntegrationTest(ITestOutputHelper outputHelper) :
        base(outputHelper, HostTestUtil.GetInMemoryHost(outputHelper).Services)
    {
    }
}