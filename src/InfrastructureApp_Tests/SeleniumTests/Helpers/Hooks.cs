using Reqnroll;
using InfrastructureApp_Tests.SeleniumTests.Helpers;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class Hooks : SeleniumTestBase
    {
        [BeforeScenario]
        public async Task BeforeScenario()
        {
            await OneTimeSetUp();
            await SetUpDriver();
        }

        [AfterScenario]
        public void AfterScenario()
        {
            TearDownDriver();
        }

        [AfterTestRun]
        public static async Task AfterTestRun()
        {
            await OneTimeTearDownStatic();
        }
    }
}
