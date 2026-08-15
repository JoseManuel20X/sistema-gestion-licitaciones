using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licitaciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "licitaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoNormalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaCierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PresupuestoEstimadoCRC = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licitaciones", x => x.Id);
                    table.CheckConstraint("ck_licitaciones_presupuesto_positivo", "\"PresupuestoEstimadoCRC\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "niveles_aprobacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MontoMinimoCRC = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoMaximoCRC = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Aprobador = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niveles_aprobacion", x => x.Id);
                    table.CheckConstraint("ck_niveles_minimo_positivo", "\"MontoMinimoCRC\" > 0");
                    table.CheckConstraint("ck_niveles_rango_coherente", "\"MontoMaximoCRC\" IS NULL OR \"MontoMaximoCRC\" > \"MontoMinimoCRC\"");
                });

            migrationBuilder.CreateTable(
                name: "proveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreNormalizado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_cambio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CRCporUSD = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    FechaVigencia = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_cambio", x => x.Id);
                    table.CheckConstraint("ck_tipos_cambio_positivo", "\"CRCporUSD\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "ofertas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicitacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MontoOfertadoCRC = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ofertas", x => x.Id);
                    table.CheckConstraint("ck_ofertas_monto_positivo", "\"MontoOfertadoCRC\" > 0");
                    table.ForeignKey(
                        name: "FK_ofertas_licitaciones_LicitacionId",
                        column: x => x.LicitacionId,
                        principalTable: "licitaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ofertas_proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_licitaciones_codigo_normalizado",
                table: "licitaciones",
                column: "CodigoNormalizado",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_licitaciones_estado",
                table: "licitaciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "ix_niveles_aprobacion_minimo",
                table: "niveles_aprobacion",
                column: "MontoMinimoCRC",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_licitacion_monto",
                table: "ofertas",
                columns: new[] { "LicitacionId", "MontoOfertadoCRC" });

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_licitacion_proveedor",
                table: "ofertas",
                columns: new[] { "LicitacionId", "ProveedorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_ProveedorId",
                table: "ofertas",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_nombre_normalizado",
                table: "proveedores",
                column: "NombreNormalizado",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tipos_cambio_activo_unico",
                table: "tipos_cambio",
                column: "Activo",
                unique: true,
                filter: "\"Activo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "niveles_aprobacion");

            migrationBuilder.DropTable(
                name: "ofertas");

            migrationBuilder.DropTable(
                name: "tipos_cambio");

            migrationBuilder.DropTable(
                name: "licitaciones");

            migrationBuilder.DropTable(
                name: "proveedores");
        }
    }
}
