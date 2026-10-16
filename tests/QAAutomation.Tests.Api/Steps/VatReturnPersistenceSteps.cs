using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using QAAutomation.Data;
using Reqnroll;

namespace QAAutomation.Tests.Api.Steps
{
    /// <summary>
    /// Steps for Features/VatReturnPersistence.feature.
    ///
    /// This is the worked example of WHERE database validation lives. The When asks the
    /// repository for a row; the Then asserts on what came back. The assertion is HERE, in the
    /// step - never in the repository, which only returns state. There is no SQL, no connection
    /// string and no selector in this file: all of that is behind VatReturnRepository.
    ///
    /// State is shared between the When and the Then through a private field. That is enough for
    /// a single-class example - Reqnroll builds one instance of this class per scenario - so no
    /// separate typed context is needed here.
    /// </summary>
    [Binding]
    public class VatReturnPersistenceSteps
    {
        private readonly VatReturnRepository vatReturnRepository;

        private VatReturnRecord storedVatReturn;

        public VatReturnPersistenceSteps(VatReturnRepository vatReturnRepository)
        {
            this.vatReturnRepository = vatReturnRepository;
        }

        // Touches the database, so it is async Task. The tenant is passed explicitly, never read
        // from ambient state - that is the cross-tenant guard VatReturnRepository documents.
        [When("the VAT return {string} for tenant {string} is read from the database")]
        public async Task WhenTheVatReturnForTenantIsReadFromTheDatabase(string returnId, string tenantId)
        {
            this.storedVatReturn = await this.vatReturnRepository.FindByIdAsync(tenantId, returnId, CancellationToken.None);
        }

        // Only asserts on already-fetched state, so it is a plain void step.
        [Then("the stored VAT return has status {string}")]
        public void ThenTheStoredVatReturnHasStatus(string expectedStatus)
        {
            this.storedVatReturn.Should().NotBeNull(
                "the VAT return written for this tenant must be readable back for it");

            this.storedVatReturn.Status.Should().Be(
                expectedStatus,
                "a persisted VAT return keeps the status it was stored with");
        }
    }
}
