using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace QAAutomation.Data
{
    /// <summary>
    /// Reads VAT return rows - "the DB class". It is the one place a SQL query for VAT returns
    /// lives. It takes a connection from the factory, runs a parameterised query and maps the row
    /// into a VatReturnRecord.
    ///
    /// It ASSERTS NOTHING. It returns the record, or null when no row matches, and the step
    /// definition does the asserting. That is the layer rule: clients and repositories return
    /// state, steps assert on it.
    /// </summary>
    public class VatReturnRepository
    {
        // Portable ANSI SQL, so the identical text runs on both PostgreSQL and SQL Server. Values
        // are NEVER concatenated into this string - tenant_id and return_id are bound as named
        // parameters in FindByIdAsync, which is what keeps the query safe from injection.
        private const string FindByIdSql =
            "SELECT tenant_id, return_id, status, total_vat_due "
            + "FROM vat_return "
            + "WHERE tenant_id = @tenantId AND return_id = @returnId";

        private const string TenantIdParameterName = "@tenantId";
        private const string ReturnIdParameterName = "@returnId";

        private const string TenantIdColumn = "tenant_id";
        private const string ReturnIdColumn = "return_id";
        private const string StatusColumn = "status";
        private const string TotalVatDueColumn = "total_vat_due";

        private readonly IDatabaseConnectionFactory connectionFactory;

        public VatReturnRepository(IDatabaseConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            this.connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Finds a single VAT return by tenant and id, or returns null when there is no such row.
        ///
        /// tenantId is a REQUIRED, explicit parameter - never read from an ambient context. A
        /// lookup that silently picks up "the current tenant" from somewhere global is the
        /// cross-tenant bug class this method exists to catch: a query that quietly returns
        /// another tenant's data. Forcing the caller to name the tenant means a test written for
        /// tenant A can never accidentally read tenant B's rows.
        /// </summary>
        public async Task<VatReturnRecord> FindByIdAsync(string tenantId, string returnId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException(
                    "A tenant id is required: a VAT return lookup must be scoped to exactly one tenant.",
                    nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(returnId))
            {
                throw new ArgumentException("A return id is required.", nameof(returnId));
            }

            await using (DbConnection connection = await this.connectionFactory.OpenAsync(cancellationToken))
            await using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = FindByIdSql;
                AddParameter(command, TenantIdParameterName, tenantId);
                AddParameter(command, ReturnIdParameterName, returnId);

                return await ReadSingleRecordAsync(command, cancellationToken);
            }
        }

        private static async Task<VatReturnRecord> ReadSingleRecordAsync(DbCommand command, CancellationToken cancellationToken)
        {
            await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                bool hasRow = await reader.ReadAsync(cancellationToken);
                if (hasRow == false)
                {
                    return null;
                }

                return MapRow(reader);
            }
        }

        private static void AddParameter(DbCommand command, string parameterName, string value)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static VatReturnRecord MapRow(DbDataReader reader)
        {
            int tenantIdOrdinal = reader.GetOrdinal(TenantIdColumn);
            int returnIdOrdinal = reader.GetOrdinal(ReturnIdColumn);
            int statusOrdinal = reader.GetOrdinal(StatusColumn);
            int totalVatDueOrdinal = reader.GetOrdinal(TotalVatDueColumn);

            VatReturnRecord record = new VatReturnRecord();
            record.TenantId = reader.GetString(tenantIdOrdinal);
            record.ReturnId = reader.GetString(returnIdOrdinal);
            record.Status = reader.GetString(statusOrdinal);
            record.TotalVatDue = reader.GetDecimal(totalVatDueOrdinal);
            return record;
        }
    }
}
