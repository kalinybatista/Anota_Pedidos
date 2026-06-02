using Microsoft.EntityFrameworkCore;
using Anota_Pedidos.Models;

namespace Anota_Pedidos.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UsuarioModel> Usuarios { get; set; }
        public DbSet<AdminModel> Admins { get; set; }
        public DbSet<EstabelecimentoModel> Estabelecimentos { get; set; }
        public DbSet<CategoriaModel> Categorias { get; set; }
        public DbSet<ProdutoModel> Produtos { get; set; }
        public DbSet<PedidoModel> Pedidos { get; set; }
        public DbSet<PedidoItemModel> PedidoItens { get; set; }
        public DbSet<TokenModel> Tokens { get; set; }
        public DbSet<SequenciaPedido> SequenciasPedido { get; set; }
        public DbSet<TokenRecuperacaoModel> TokensRecuperacao { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar nomes das tabelas
            modelBuilder.Entity<UsuarioModel>().ToTable("Usuarios");
            modelBuilder.Entity<AdminModel>().ToTable("Admins");
            modelBuilder.Entity<EstabelecimentoModel>().ToTable("Estabelecimento");
            modelBuilder.Entity<CategoriaModel>().ToTable("Categoria");
            modelBuilder.Entity<ProdutoModel>().ToTable("Produtos");
            modelBuilder.Entity<PedidoModel>().ToTable("Pedidos");
            modelBuilder.Entity<PedidoItemModel>().ToTable("Pedido_Itens");
            modelBuilder.Entity<TokenModel>().ToTable("Tokens");

            // No OnModelCreating
            modelBuilder.Entity<UsuarioModel>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(e => e.Id_Usuario);

                // Mapear Telefone para a coluna criptografada
                entity.Property(e => e.Telefone)
                    .HasColumnName("Telefone")
                    .HasMaxLength(255);  // Espaço suficiente para texto criptografado
            });

            // Configurar precisão para valores decimais
            modelBuilder.Entity<ProdutoModel>()
                .Property(p => p.Valor_Produto)
                .HasPrecision(10, 2);

            //modelBuilder.Entity<PedidoModel>()
            //    .Property(p => p.Status_Pedido)
            //    .HasConversion<string>()
            //    .HasMaxLength(20);

            modelBuilder.Entity<PedidoItemModel>()
                .Property(p => p.Valor_Unitario)
                .HasPrecision(10, 2);

            // Configurar enum para Status_Pedido
            modelBuilder.Entity<PedidoModel>()
                .Property(p => p.Status_Pedido)
                .HasConversion<string>()
                .HasMaxLength(20);


            // Configurar campos opcionais
            modelBuilder.Entity<CategoriaModel>()
                .Property(c => c.Descricao_Categoria)
                .IsRequired(false);

            modelBuilder.Entity<ProdutoModel>()
                .Property(p => p.Descricao_Produto)
                .IsRequired(false);

            modelBuilder.Entity<ProdutoModel>()
                .Property(p => p.Img_Produto)
                .IsRequired(false);

            modelBuilder.Entity<AdminModel>()
                .Property(a => a.Img_Admin)
                .IsRequired(false);

            modelBuilder.Entity<EstabelecimentoModel>()
                .Property(e => e.Img_Estabelecimento)
                .IsRequired(false);

            modelBuilder.Entity<PedidoModel>()
                .Property(p => p.FormaPagamento)
                .IsRequired(false);

            modelBuilder.Entity<PedidoModel>()
                .Property(p => p.TipoPedido)
                .IsRequired(false);

            modelBuilder.Entity<PedidoModel>()
                .Property(p => p.Valor_Total)
                .IsRequired(false);

            // Ignorar propriedades IFormFile
            modelBuilder.Entity<EstabelecimentoModel>()
                .Ignore(e => e.ImagemArquivo);

            modelBuilder.Entity<ProdutoModel>()
                .Ignore(p => p.ImagemArquivo);

            modelBuilder.Entity<AdminModel>()
                .Ignore(a => a.ImagemArquivo);

            modelBuilder.Entity<EstabelecimentoModel>()
                .Property(e => e.WhatsApp)
                .IsRequired(false)
                .HasMaxLength(20);
        }
    }
}