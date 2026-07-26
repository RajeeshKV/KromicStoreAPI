using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveryLogs_NextRetryAt_Pending",
                table: "WebhookDeliveryLogs");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Status_Active",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_Status_Active",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "FailedPaymentCount",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaymentDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextPaymentDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Subscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "RazorpayCustomerId",
                table: "Subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazorpaySubscriptionId",
                table: "Subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RazorpayOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RazorpayPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RazorpaySubscriptionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RazorpaySubscriptionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RazorpayEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RazorpaySubscriptionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RazorpaySubscriptionEvents_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantPaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "text", nullable: false),
                    EncryptedApiSecret = table.Column<string>(type: "text", nullable: false),
                    EncryptedWebhookSecret = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TestModeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantPaymentMethods_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_NextRetryAt_Pending",
                table: "WebhookDeliveryLogs",
                column: "NextRetryAt",
                filter: "\"NextRetryAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_RazorpaySubscriptionId",
                table: "Subscriptions",
                column: "RazorpaySubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_PaymentStatus",
                table: "Subscriptions",
                columns: new[] { "TenantId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Status_Active",
                table: "Products",
                columns: new[] { "TenantId", "Status" },
                filter: "\"Status\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_Status_Active",
                table: "Orders",
                columns: new[] { "TenantId", "Status" },
                filter: "\"Status\" IN (0, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_CreatedAt",
                table: "OrderPayments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_OrderId",
                table: "OrderPayments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_Status",
                table: "OrderPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_TenantId",
                table: "OrderPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_TenantId_Status",
                table: "OrderPayments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_OrderPayments_RazorpayOrderId",
                table: "OrderPayments",
                column: "RazorpayOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RazorpaySubscriptionEvents_CreatedAt",
                table: "RazorpaySubscriptionEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RazorpaySubscriptionEvents_EventType",
                table: "RazorpaySubscriptionEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_RazorpaySubscriptionEvents_SubscriptionId",
                table: "RazorpaySubscriptionEvents",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "UX_RazorpaySubscriptionEvents_RazorpayEventId",
                table: "RazorpaySubscriptionEvents",
                column: "RazorpayEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPaymentMethods_IsEnabled",
                table: "TenantPaymentMethods",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPaymentMethods_TenantId",
                table: "TenantPaymentMethods",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_TenantPaymentMethods_TenantId_Provider",
                table: "TenantPaymentMethods",
                columns: new[] { "TenantId", "Provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPayments");

            migrationBuilder.DropTable(
                name: "RazorpaySubscriptionEvents");

            migrationBuilder.DropTable(
                name: "TenantPaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveryLogs_NextRetryAt_Pending",
                table: "WebhookDeliveryLogs");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_RazorpaySubscriptionId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_PaymentStatus",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Status_Active",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_Status_Active",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FailedPaymentCount",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastPaymentDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "NextPaymentDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RazorpayCustomerId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RazorpaySubscriptionId",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_NextRetryAt_Pending",
                table: "WebhookDeliveryLogs",
                column: "NextRetryAt",
                filter: "[NextRetryAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Status_Active",
                table: "Products",
                columns: new[] { "TenantId", "Status" },
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_Status_Active",
                table: "Orders",
                columns: new[] { "TenantId", "Status" },
                filter: "[Status] IN (0, 2, 3)");
        }
    }
}
