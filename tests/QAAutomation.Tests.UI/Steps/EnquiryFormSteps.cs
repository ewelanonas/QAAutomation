using AwesomeAssertions;
using QAAutomation.UI.Pages;
using Reqnroll;

namespace QAAutomation.Tests.UI.Steps
{
    /// <summary>
    /// Steps for Features/EnquiryForm.feature.
    ///
    /// READ-ONLY, DELIBERATELY. The target is a production website and the form is a real sales
    /// enquiry form, so nothing here submits it. There is no submit step because ContactUsPage
    /// has no submit method to call - see the comment at the top of that class and the block at
    /// the top of the feature file.
    ///
    /// Plain void methods, because Selenium is synchronous.
    /// </summary>
    [Binding]
    public class EnquiryFormSteps
    {
        private readonly ContactUsPage contactUsPage;
        private readonly EnquiryFormContext enquiryFormContext;

        public EnquiryFormSteps(ContactUsPage contactUsPage, EnquiryFormContext enquiryFormContext)
        {
            this.contactUsPage = contactUsPage;
            this.enquiryFormContext = enquiryFormContext;
        }

        [Given("the visitor opens the Landmark contact page")]
        public void GivenTheVisitorOpensTheLandmarkContactPage()
        {
            this.contactUsPage.Open();
        }

        // Written as a regular expression rather than the usual {string} shorthand because the
        // second value is a true/false outcome read straight from the Examples table, and the
        // (true|false) group both reads clearly in the feature file and refuses anything else.
        [Then(@"^the ""(.*)"" field is mandatory: (true|false)$")]
        public void ThenTheFieldIsMandatory(string fieldName, bool expectedMandatory)
        {
            this.enquiryFormContext.FieldIsMandatory = this.contactUsPage.IsFieldMandatory(fieldName);

            // This reads the declaration the form itself publishes (aria-required), which is
            // also what a screen reader announces to a visitor who cannot see the asterisk. It
            // is the mandatory-field contract, checked without submitting anything.
            this.enquiryFormContext.FieldIsMandatory.Should().Be(
                expectedMandatory,
                "the form must tell a visitor which details are required before they start typing");
        }

        [When("the visitor types {string} into the {string} field")]
        public void WhenTheVisitorTypesIntoTheField(string value, string fieldName)
        {
            this.contactUsPage.TypeInto(fieldName, value);
        }

        [Then("the {string} field holds {string}")]
        public void ThenTheFieldHolds(string fieldName, string expectedValue)
        {
            this.enquiryFormContext.ValueHeldByField = this.contactUsPage.ReadValueOf(fieldName);

            this.enquiryFormContext.ValueHeldByField.Should().Be(
                expectedValue,
                "a form that loses what a visitor typed loses the enquiry with it");
        }
    }
}
