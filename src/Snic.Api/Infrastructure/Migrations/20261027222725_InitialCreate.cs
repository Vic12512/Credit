using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Snic.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_event_types",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "institution_types",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institution_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_statuses",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_statuses",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_types",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "score_rules",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    min_days_past_due = table.Column<int>(type: "int", nullable: false),
                    max_days_past_due = table.Column<int>(type: "int", nullable: true),
                    score = table.Column<short>(type: "smallint", nullable: false),
                    risk_level = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    estimated_default_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_score_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "institutions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    institution_type_id = table.Column<short>(type: "smallint", nullable: false),
                    code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    rnc = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    legal_name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    commercial_name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivation_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institutions", x => x.id);
                    table.ForeignKey(
                        name: "FK_institutions_institution_types_institution_type_id",
                        column: x => x.institution_type_id,
                        principalTable: "institution_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    institution_id = table.Column<int>(type: "int", nullable: true),
                    role_id = table.Column<short>(type: "smallint", nullable: false),
                    username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    failed_login_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    locked_until = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivation_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.id);
                    table.CheckConstraint("CK_user_accounts_role_institution", "([role_id] = 1 AND [institution_id] IS NULL) OR ([role_id] <> 1 AND [institution_id] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_user_accounts_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_accounts_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    national_id = table.Column<string>(type: "char(11)", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    middle_name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    first_surname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    second_surname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by_account_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivation_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persons", x => x.id);
                    table.ForeignKey(
                        name: "FK_persons_user_accounts_created_by_account_id",
                        column: x => x.created_by_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account_id = table.Column<long>(type: "bigint", nullable: true),
                    institution_id = table.Column<int>(type: "int", nullable: true),
                    person_id = table.Column<long>(type: "bigint", nullable: true),
                    operation = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    entity_type = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    entity_id = table.Column<long>(type: "bigint", nullable: true),
                    result = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    attempted_username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    old_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    new_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ip_address = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_logs_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_logs_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_products",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    institution_id = table.Column<int>(type: "int", nullable: false),
                    product_type_id = table.Column<short>(type: "smallint", nullable: false),
                    product_status_id = table.Column<short>(type: "smallint", nullable: false),
                    external_product_identifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    approved_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    current_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    currency_code = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "DOP"),
                    opening_date = table.Column<DateOnly>(type: "date", nullable: false),
                    closing_date = table.Column<DateOnly>(type: "date", nullable: true),
                    latest_report_date = table.Column<DateOnly>(type: "date", nullable: true),
                    latest_days_past_due = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_by_account_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivation_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_products_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_products_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_products_product_statuses_product_status_id",
                        column: x => x.product_status_id,
                        principalTable: "product_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_products_product_types_product_type_id",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_products_user_accounts_created_by_account_id",
                        column: x => x.created_by_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_history_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    credit_product_id = table.Column<long>(type: "bigint", nullable: false),
                    credit_event_type_id = table.Column<short>(type: "smallint", nullable: false),
                    payment_status_id = table.Column<short>(type: "smallint", nullable: true),
                    product_status_after_id = table.Column<short>(type: "smallint", nullable: true),
                    event_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: true),
                    period_end = table.Column<DateOnly>(type: "date", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    amount_due = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    amount_paid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    balance_after = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    days_past_due = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    source_reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    reported_by_account_id = table.Column<long>(type: "bigint", nullable: false),
                    reported_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    supersedes_event_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    deactivated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deactivation_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_history_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_history_events_credit_event_types_credit_event_type_id",
                        column: x => x.credit_event_type_id,
                        principalTable: "credit_event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_history_events_credit_history_events_supersedes_event_id",
                        column: x => x.supersedes_event_id,
                        principalTable: "credit_history_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_history_events_credit_products_credit_product_id",
                        column: x => x.credit_product_id,
                        principalTable: "credit_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_history_events_payment_statuses_payment_status_id",
                        column: x => x.payment_status_id,
                        principalTable: "payment_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_history_events_product_statuses_product_status_after_id",
                        column: x => x.product_status_after_id,
                        principalTable: "product_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_history_events_user_accounts_reported_by_account_id",
                        column: x => x.reported_by_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "credit_event_types",
                columns: new[] { "id", "code", "description", "is_active", "name" },
                values: new object[,]
                {
                    { (short)1, "PRODUCT_OPENED", null, true, "Producto abierto" },
                    { (short)2, "PERIOD_REPORTED", null, true, "Período reportado" },
                    { (short)3, "BALANCE_UPDATED", null, true, "Saldo actualizado" },
                    { (short)4, "PAYMENT_REPORTED", null, true, "Pago reportado" },
                    { (short)5, "DELINQUENCY_REPORTED", null, true, "Mora reportada" },
                    { (short)6, "PRODUCT_CLOSED", null, true, "Producto cerrado" },
                    { (short)7, "PRODUCT_CANCELLED", null, true, "Producto cancelado" }
                });

            migrationBuilder.InsertData(
                table: "institution_types",
                columns: new[] { "id", "code", "is_active", "name" },
                values: new object[,]
                {
                    { (short)1, "BANK", true, "Banco" },
                    { (short)2, "COOPERATIVE", true, "Cooperativa" },
                    { (short)3, "FINANCIAL_ENTITY", true, "Entidad financiera" },
                    { (short)4, "COMMERCIAL_COMPANY", true, "Empresa comercial" }
                });

            migrationBuilder.InsertData(
                table: "payment_statuses",
                columns: new[] { "id", "code", "is_active", "name" },
                values: new object[,]
                {
                    { (short)1, "PENDING", true, "Pendiente" },
                    { (short)2, "PARTIAL", true, "Parcial" },
                    { (short)3, "PAID_ON_TIME", true, "Pagado a tiempo" },
                    { (short)4, "PAID_LATE", true, "Pagado tarde" },
                    { (short)5, "OVERDUE", true, "Vencido" }
                });

            migrationBuilder.InsertData(
                table: "product_statuses",
                columns: new[] { "id", "code", "is_active", "name" },
                values: new object[,]
                {
                    { (short)1, "OPEN", true, "Abierto" },
                    { (short)2, "CLOSED", true, "Cerrado" },
                    { (short)3, "CANCELLED", true, "Cancelado" }
                });

            migrationBuilder.InsertData(
                table: "product_types",
                columns: new[] { "id", "code", "is_active", "name" },
                values: new object[,]
                {
                    { (short)1, "CREDIT_CARD", true, "Tarjeta de crédito" },
                    { (short)2, "LOAN", true, "Préstamo" },
                    { (short)3, "MORTGAGE", true, "Hipoteca" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "description", "is_active", "name" },
                values: new object[,]
                {
                    { (short)1, "ADMINISTRATOR", null, true, "Administrador" },
                    { (short)2, "CONSUMER", null, true, "Consumidor" },
                    { (short)3, "DATA_ENTRY", null, true, "Registrador" }
                });

            migrationBuilder.InsertData(
                table: "score_rules",
                columns: new[] { "id", "estimated_default_percentage", "is_active", "max_days_past_due", "min_days_past_due", "risk_level", "score" },
                values: new object[,]
                {
                    { (short)1, 0m, true, 0, 0, "LOW", (short)100 },
                    { (short)2, 20m, true, 30, 1, "LOW", (short)80 },
                    { (short)3, 40m, true, 60, 31, "MEDIUM", (short)60 },
                    { (short)4, 60m, true, 90, 61, "HIGH", (short)40 },
                    { (short)5, 80m, true, null, 91, "HIGH", (short)20 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_account_date",
                table: "audit_logs",
                columns: new[] { "account_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_date",
                table: "audit_logs",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_institution_date",
                table: "audit_logs",
                columns: new[] { "institution_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_operation_result_date",
                table: "audit_logs",
                columns: new[] { "operation", "result", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_person_date",
                table: "audit_logs",
                columns: new[] { "person_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_event_types_code",
                table: "credit_event_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_events_credit_event_type_id",
                table: "credit_history_events",
                column: "credit_event_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_events_payment_status_id",
                table: "credit_history_events",
                column: "payment_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_events_product_status_after_id",
                table: "credit_history_events",
                column: "product_status_after_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_events_supersedes_event_id",
                table: "credit_history_events",
                column: "supersedes_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_product_date",
                table: "credit_history_events",
                columns: new[] { "credit_product_id", "event_date" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_product_source_reference",
                table: "credit_history_events",
                columns: new[] { "credit_product_id", "source_reference" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_reporter_date",
                table: "credit_history_events",
                columns: new[] { "reported_by_account_id", "reported_at" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_history_score_window",
                table: "credit_history_events",
                columns: new[] { "event_date", "days_past_due" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_products_created_by_account_id",
                table: "credit_products",
                column: "created_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_products_institution_status",
                table: "credit_products",
                columns: new[] { "institution_id", "product_status_id" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_products_person_active",
                table: "credit_products",
                columns: new[] { "person_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_products_person_institution_type",
                table: "credit_products",
                columns: new[] { "person_id", "institution_id", "product_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_products_product_status_id",
                table: "credit_products",
                column: "product_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_products_product_type_id",
                table: "credit_products",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "UQ_credit_products_institution_external_id",
                table: "credit_products",
                columns: new[] { "institution_id", "external_product_identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_institution_types_code",
                table: "institution_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_institutions_code",
                table: "institutions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_institutions_rnc",
                table: "institutions",
                column: "rnc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_institutions_type_active",
                table: "institutions",
                columns: new[] { "institution_type_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_statuses_code",
                table: "payment_statuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persons_active",
                table: "persons",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_persons_created_by_account_id",
                table: "persons",
                column: "created_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_persons_name",
                table: "persons",
                columns: new[] { "first_surname", "first_name" });

            migrationBuilder.CreateIndex(
                name: "IX_persons_national_id",
                table: "persons",
                column: "national_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_statuses_code",
                table: "product_statuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_types_code",
                table: "product_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_score_rules_range",
                table: "score_rules",
                columns: new[] { "min_days_past_due", "max_days_past_due" },
                unique: true,
                filter: "[max_days_past_due] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_email",
                table: "user_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_institution_active",
                table: "user_accounts",
                columns: new[] { "institution_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_role_active",
                table: "user_accounts",
                columns: new[] { "role_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_username",
                table: "user_accounts",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "credit_history_events");

            migrationBuilder.DropTable(
                name: "score_rules");

            migrationBuilder.DropTable(
                name: "credit_event_types");

            migrationBuilder.DropTable(
                name: "credit_products");

            migrationBuilder.DropTable(
                name: "payment_statuses");

            migrationBuilder.DropTable(
                name: "persons");

            migrationBuilder.DropTable(
                name: "product_statuses");

            migrationBuilder.DropTable(
                name: "product_types");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "institutions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "institution_types");
        }
    }
}
