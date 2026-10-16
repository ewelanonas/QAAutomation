namespace QAAutomation.Data
{
    /// <summary>
    /// One row from the vat_return table. A plain data carrier: properties only, no behaviour and
    /// no assertions. The repository maps a database row into this and hands it back; the step
    /// definition is where it is asserted on.
    /// </summary>
    public class VatReturnRecord
    {
        public string ReturnId { get; set; }

        public string TenantId { get; set; }

        public string Status { get; set; }

        public decimal TotalVatDue { get; set; }
    }
}
