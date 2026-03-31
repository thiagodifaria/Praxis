using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Praxis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddV3OperationalModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "warehouse_locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDecidedAtUtc",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalRequestedAtUtc",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalRuleId",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "sales_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByName",
                table: "sales_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "receivables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "receivables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDecidedAtUtc",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalRequestedAtUtc",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalRuleId",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "purchase_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByName",
                table: "purchase_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "payables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "payables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "operational_alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Document = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsHeadquarters = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "approval_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiredRoleName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_rules_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cost_centers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cost_centers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cost_centers_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "module_feature_flags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_feature_flags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_module_feature_flags_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "realtime_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_realtime_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_realtime_notifications_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_realtime_notifications_users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approval_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DecisionByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_decisions_approval_rules_ApprovalRuleId",
                        column: x => x.ApprovalRuleId,
                        principalTable: "approval_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_approval_decisions_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_approval_decisions_cost_centers_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "cost_centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_locations_BranchId",
                table: "warehouse_locations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_ApprovalRuleId",
                table: "sales_orders",
                column: "ApprovalRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_BranchId",
                table: "sales_orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_CostCenterId",
                table: "sales_orders",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_receivables_BranchId",
                table: "receivables",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_receivables_CostCenterId",
                table: "receivables",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_ApprovalRuleId",
                table: "purchase_orders",
                column: "ApprovalRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_BranchId",
                table: "purchase_orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CostCenterId",
                table: "purchase_orders",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_payables_BranchId",
                table: "payables",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_payables_CostCenterId",
                table: "payables",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_operational_alerts_BranchId",
                table: "operational_alerts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_BranchId",
                table: "invoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_CostCenterId",
                table: "invoices",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_decisions_ApprovalRuleId",
                table: "approval_decisions",
                column: "ApprovalRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_decisions_BranchId",
                table: "approval_decisions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_decisions_CostCenterId",
                table: "approval_decisions",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_decisions_Module_EntityId_Status",
                table: "approval_decisions",
                columns: new[] { "Module", "EntityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_rules_BranchId",
                table: "approval_rules",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_rules_Module_BranchId_MinimumAmount",
                table: "approval_rules",
                columns: new[] { "Module", "BranchId", "MinimumAmount" });

            migrationBuilder.CreateIndex(
                name: "IX_branches_Code",
                table: "branches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cost_centers_BranchId_Code",
                table: "cost_centers",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_module_feature_flags_BranchId",
                table: "module_feature_flags",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_module_feature_flags_ModuleKey_BranchId",
                table: "module_feature_flags",
                columns: new[] { "ModuleKey", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_realtime_notifications_BranchId",
                table: "realtime_notifications",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_realtime_notifications_IsRead_PublishedAtUtc",
                table: "realtime_notifications",
                columns: new[] { "IsRead", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_realtime_notifications_RecipientUserId",
                table: "realtime_notifications",
                column: "RecipientUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_branches_BranchId",
                table: "invoices",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_cost_centers_CostCenterId",
                table: "invoices",
                column: "CostCenterId",
                principalTable: "cost_centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_operational_alerts_branches_BranchId",
                table: "operational_alerts",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payables_branches_BranchId",
                table: "payables",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payables_cost_centers_CostCenterId",
                table: "payables",
                column: "CostCenterId",
                principalTable: "cost_centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_approval_rules_ApprovalRuleId",
                table: "purchase_orders",
                column: "ApprovalRuleId",
                principalTable: "approval_rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_branches_BranchId",
                table: "purchase_orders",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_cost_centers_CostCenterId",
                table: "purchase_orders",
                column: "CostCenterId",
                principalTable: "cost_centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_receivables_branches_BranchId",
                table: "receivables",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_receivables_cost_centers_CostCenterId",
                table: "receivables",
                column: "CostCenterId",
                principalTable: "cost_centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_orders_approval_rules_ApprovalRuleId",
                table: "sales_orders",
                column: "ApprovalRuleId",
                principalTable: "approval_rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_orders_branches_BranchId",
                table: "sales_orders",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_orders_cost_centers_CostCenterId",
                table: "sales_orders",
                column: "CostCenterId",
                principalTable: "cost_centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouse_locations_branches_BranchId",
                table: "warehouse_locations",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_branches_BranchId",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_cost_centers_CostCenterId",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_operational_alerts_branches_BranchId",
                table: "operational_alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_payables_branches_BranchId",
                table: "payables");

            migrationBuilder.DropForeignKey(
                name: "FK_payables_cost_centers_CostCenterId",
                table: "payables");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_approval_rules_ApprovalRuleId",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_branches_BranchId",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_cost_centers_CostCenterId",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_receivables_branches_BranchId",
                table: "receivables");

            migrationBuilder.DropForeignKey(
                name: "FK_receivables_cost_centers_CostCenterId",
                table: "receivables");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_orders_approval_rules_ApprovalRuleId",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_orders_branches_BranchId",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_orders_cost_centers_CostCenterId",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouse_locations_branches_BranchId",
                table: "warehouse_locations");

            migrationBuilder.DropTable(
                name: "approval_decisions");

            migrationBuilder.DropTable(
                name: "module_feature_flags");

            migrationBuilder.DropTable(
                name: "realtime_notifications");

            migrationBuilder.DropTable(
                name: "approval_rules");

            migrationBuilder.DropTable(
                name: "cost_centers");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_locations_BranchId",
                table: "warehouse_locations");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_ApprovalRuleId",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_BranchId",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_CostCenterId",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_receivables_BranchId",
                table: "receivables");

            migrationBuilder.DropIndex(
                name: "IX_receivables_CostCenterId",
                table: "receivables");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_ApprovalRuleId",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_BranchId",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_CostCenterId",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_payables_BranchId",
                table: "payables");

            migrationBuilder.DropIndex(
                name: "IX_payables_CostCenterId",
                table: "payables");

            migrationBuilder.DropIndex(
                name: "IX_operational_alerts_BranchId",
                table: "operational_alerts");

            migrationBuilder.DropIndex(
                name: "IX_invoices_BranchId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_CostCenterId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "warehouse_locations");

            migrationBuilder.DropColumn(
                name: "ApprovalDecidedAtUtc",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalRequestedAtUtc",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalRuleId",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedByName",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "receivables");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "receivables");

            migrationBuilder.DropColumn(
                name: "ApprovalDecidedAtUtc",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalRequestedAtUtc",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalRuleId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedByName",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "payables");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "payables");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "operational_alerts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "invoices");
        }
    }
}
