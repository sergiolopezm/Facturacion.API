using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.API.Infrastructure;

public partial class DBContext : DbContext
{
    public DBContext()
    {
    }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Acceso> Accesos { get; set; }

    public virtual DbSet<Articulo> Articulos { get; set; }

    public virtual DbSet<CategoriasArticulo> CategoriasArticulos { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<FacturaDetalle> FacturaDetalles { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<TokensExpirado> TokensExpirados { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VwFacturaDetallesCompleto> VwFacturaDetallesCompletos { get; set; }

    public virtual DbSet<VwFacturasCompleta> VwFacturasCompletas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Acceso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Accesos__3214EC07124FCAE2");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Contraseña).HasMaxLength(250);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Sitio).HasMaxLength(50);
        });

        modelBuilder.Entity<Articulo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Articulo__3214EC0772E57554");

            entity.HasIndex(e => e.Activo, "IX_Articulos_Activo");

            entity.HasIndex(e => e.CategoriaId, "IX_Articulos_CategoriaId");

            entity.HasIndex(e => e.Codigo, "IX_Articulos_Codigo");

            entity.HasIndex(e => e.Codigo, "UQ__Articulo__06370DAC5DF5B711").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Articulos)
                .HasForeignKey(d => d.CategoriaId)
                .HasConstraintName("FK__Articulos__Categ__656C112C");

            entity.HasOne(d => d.CreadoPor).WithMany(p => p.ArticuloCreadoPors)
                .HasForeignKey(d => d.CreadoPorId)
                .HasConstraintName("FK__Articulos__Cread__66603565");

            entity.HasOne(d => d.ModificadoPor).WithMany(p => p.ArticuloModificadoPors)
                .HasForeignKey(d => d.ModificadoPorId)
                .HasConstraintName("FK__Articulos__Modif__6754599E");
        });

        modelBuilder.Entity<CategoriasArticulo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC0703C5909B");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Nombre).HasMaxLength(100);

            entity.HasOne(d => d.CreadoPor).WithMany(p => p.CategoriasArticuloCreadoPors)
                .HasForeignKey(d => d.CreadoPorId)
                .HasConstraintName("FK__Categoria__Cread__5CD6CB2B");

            entity.HasOne(d => d.ModificadoPor).WithMany(p => p.CategoriasArticuloModificadoPors)
                .HasForeignKey(d => d.ModificadoPorId)
                .HasConstraintName("FK__Categoria__Modif__5DCAEF64");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Clientes__3214EC07381A89FE");

            entity.HasIndex(e => e.Activo, "IX_Clientes_Activo");

            entity.HasIndex(e => e.NumeroDocumento, "IX_Clientes_NumeroDocumento");

            entity.HasIndex(e => e.NumeroDocumento, "UQ__Clientes__A420258861E65867").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellidos).HasMaxLength(100);
            entity.Property(e => e.Direccion).HasMaxLength(250);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Nombres).HasMaxLength(100);
            entity.Property(e => e.NumeroDocumento).HasMaxLength(15);
            entity.Property(e => e.Telefono).HasMaxLength(20);

            entity.HasOne(d => d.CreadoPor).WithMany(p => p.ClienteCreadoPors)
                .HasForeignKey(d => d.CreadoPorId)
                .HasConstraintName("FK__Clientes__Creado__571DF1D5");

            entity.HasOne(d => d.ModificadoPor).WithMany(p => p.ClienteModificadoPors)
                .HasForeignKey(d => d.ModificadoPorId)
                .HasConstraintName("FK__Clientes__Modifi__5812160E");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Facturas__3214EC07C5D3AB0C");

            entity.ToTable(tb => tb.HasTrigger("TR_ActualizarStockFactura"));

            entity.HasIndex(e => e.ClienteId, "IX_Facturas_ClienteId");

            entity.HasIndex(e => e.Estado, "IX_Facturas_Estado");

            entity.HasIndex(e => e.Fecha, "IX_Facturas_Fecha");

            entity.HasIndex(e => e.ClienteNumeroDocumento, "IX_Facturas_NumeroDocumento");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.BaseImpuestos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ClienteApellidos).HasMaxLength(100);
            entity.Property(e => e.ClienteDireccion).HasMaxLength(250);
            entity.Property(e => e.ClienteNombres).HasMaxLength(100);
            entity.Property(e => e.ClienteNumeroDocumento).HasMaxLength(15);
            entity.Property(e => e.ClienteTelefono).HasMaxLength(20);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Activa");
            entity.Property(e => e.Fecha).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NumeroFactura)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComputedColumnSql("('FAC-'+right('000000'+CONVERT([varchar](6),[Id]),(6)))", true);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PorcentajeIva)
                .HasDefaultValue(19m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("PorcentajeIVA");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDescuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorIva)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorIVA");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Facturas__Client__70DDC3D8");

            entity.HasOne(d => d.CreadoPor).WithMany(p => p.FacturaCreadoPors)
                .HasForeignKey(d => d.CreadoPorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Facturas__Creado__71D1E811");

            entity.HasOne(d => d.ModificadoPor).WithMany(p => p.FacturaModificadoPors)
                .HasForeignKey(d => d.ModificadoPorId)
                .HasConstraintName("FK__Facturas__Modifi__72C60C4A");
        });

        modelBuilder.Entity<FacturaDetalle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FacturaD__3214EC075CD4EB70");

            entity.ToTable(tb => tb.HasTrigger("TR_ValidarStockFacturaDetalle"));

            entity.HasIndex(e => e.ArticuloId, "IX_FacturaDetalles_ArticuloId");

            entity.HasIndex(e => e.FacturaId, "IX_FacturaDetalles_FacturaId");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ArticuloCodigo).HasMaxLength(20);
            entity.Property(e => e.ArticuloDescripcion).HasMaxLength(250);
            entity.Property(e => e.ArticuloNombre).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Articulo).WithMany(p => p.FacturaDetalles)
                .HasForeignKey(d => d.ArticuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FacturaDe__Artic__787EE5A0");

            entity.HasOne(d => d.Factura).WithMany(p => p.FacturaDetalles)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FacturaDe__Factu__778AC167");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Logs__3214EC0787546D5B");

            entity.Property(e => e.Accion).HasMaxLength(200);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Ip).HasMaxLength(45);
            entity.Property(e => e.Tipo).HasMaxLength(50);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK__Logs__UsuarioId__5165187F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0769596646");

            entity.HasIndex(e => e.Nombre, "UQ__Roles__75E3EFCFDCC23110").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tokens__3214EC07CC170E30");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Ip).HasMaxLength(45);
            entity.Property(e => e.Observacion).HasMaxLength(200);
            entity.Property(e => e.Token1)
                .HasMaxLength(1000)
                .HasColumnName("Token");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Tokens)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tokens__UsuarioI__49C3F6B7");
        });

        modelBuilder.Entity<TokensExpirado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TokensEx__3214EC07EF60FC86");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Ip).HasMaxLength(45);
            entity.Property(e => e.Observacion).HasMaxLength(200);
            entity.Property(e => e.Token).HasMaxLength(1000);

            entity.HasOne(d => d.Usuario).WithMany(p => p.TokensExpirados)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TokensExp__Usuar__4D94879B");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC076F995926");

            entity.HasIndex(e => e.NombreUsuario, "UQ__Usuarios__6B0F5AE0A22CC754").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Usuarios__A9D1053489FE8D8B").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Contraseña).HasMaxLength(250);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.NombreUsuario).HasMaxLength(100);

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuarios__RolId__44FF419A");
        });

        modelBuilder.Entity<VwFacturaDetallesCompleto>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_FacturaDetallesCompletos");

            entity.Property(e => e.ArticuloCodigo).HasMaxLength(20);
            entity.Property(e => e.ArticuloDescripcion).HasMaxLength(250);
            entity.Property(e => e.ArticuloNombre).HasMaxLength(100);
            entity.Property(e => e.CategoriaArticulo).HasMaxLength(100);
            entity.Property(e => e.NumeroFactura)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VwFacturasCompleta>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_FacturasCompletas");

            entity.Property(e => e.BaseImpuestos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ClienteApellidos).HasMaxLength(100);
            entity.Property(e => e.ClienteDireccion).HasMaxLength(250);
            entity.Property(e => e.ClienteNombreCompleto).HasMaxLength(201);
            entity.Property(e => e.ClienteNombres).HasMaxLength(100);
            entity.Property(e => e.ClienteNumeroDocumento).HasMaxLength(15);
            entity.Property(e => e.ClienteTelefono).HasMaxLength(20);
            entity.Property(e => e.CreadoPor).HasMaxLength(201);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.ModificadoPor).HasMaxLength(201);
            entity.Property(e => e.NumeroFactura)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PorcentajeIva)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("PorcentajeIVA");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDescuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorIva)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorIVA");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
