namespace QAAutomation.Tests.UI
{
    /// <summary>
    /// What an enquiry-form scenario remembers between its steps.
    ///
    /// Typed properties for the same reason as every other context in this repository: a
    /// mistyped property name is a build error, a mistyped dictionary key is a puzzle at run
    /// time. Registered scoped, so every scenario starts with an empty one.
    /// </summary>
    public class EnquiryFormContext
    {
        /// <summary>Whether the form declared the field as one the visitor must fill in.</summary>
        public bool FieldIsMandatory { get; set; }

        /// <summary>What the field was holding when it was last read.</summary>
        public string ValueHeldByField { get; set; }
    }
}
