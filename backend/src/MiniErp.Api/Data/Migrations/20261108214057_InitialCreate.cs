using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniErp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    unit = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    cost_price = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    stock = table.Column<int>(type: "integer", nullable: false),
                    min_stock = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    customer_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    customer_document = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_orders_users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    sku = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_items_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_products_name",
                table: "products",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_products_sku",
                table: "products",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_items_product_id",
                table: "sales_order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_items_sales_order_id",
                table: "sales_order_items",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_number",
                table: "sales_orders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_seller_id",
                table: "sales_orders",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_status_created_at",
                table: "sales_orders",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_created_at",
                table: "stock_movements",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_product_id_created_at",
                table: "stock_movements",
                columns: new[] { "product_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_sales_order_id",
                table: "stock_movements",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_user_id",
                table: "stock_movements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_order_items");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "sales_orders");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
