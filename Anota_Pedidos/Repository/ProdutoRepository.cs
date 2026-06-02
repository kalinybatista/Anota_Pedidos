using Microsoft.EntityFrameworkCore;
using Anota_Pedidos.Data;
using Anota_Pedidos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Anota_Pedidos.Repository
{
    public class ProdutoRepository : IRepository<ProdutoModel>
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<ProdutoModel> _dbSet;

        public ProdutoRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<ProdutoModel>();
        }

        public async Task<ProdutoModel> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id_Produto == id);
        }

        public async Task<IEnumerable<ProdutoModel>> GetAllAsync()
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .OrderBy(p => p.Categoria.Nome_Categoria)
                .ThenBy(p => p.Nome_Produto)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProdutoModel>> FindAsync(Expression<Func<ProdutoModel, bool>> predicate)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<ProdutoModel> AddAsync(ProdutoModel entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // CORREÇÃO: Método UpdateAsync corrigido
        public async Task UpdateAsync(ProdutoModel entity)
        {
            try
            {
                // Buscar o produto existente no banco
                var existingProduto = await _dbSet
                    .FirstOrDefaultAsync(p => p.Id_Produto == entity.Id_Produto);

                if (existingProduto == null)
                    throw new Exception($"Produto com ID {entity.Id_Produto} não encontrado");

                // Atualizar propriedade por propriedade
                existingProduto.Nome_Produto = entity.Nome_Produto;
                existingProduto.Descricao_Produto = entity.Descricao_Produto;
                existingProduto.Valor_Produto = entity.Valor_Produto;
                existingProduto.Id_Categoria = entity.Id_Categoria;
                existingProduto.Img_Produto = entity.Img_Produto;

                // Marcar como modificado (opcional, o EF já vai rastrear as mudanças)
                _context.Entry(existingProduto).State = EntityState.Modified;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao atualizar produto: {ex.Message}");
            }
        }

        // Método alternativo usando Attach (útil quando você tem certeza que a entidade existe)
        public async Task UpdateAsyncAttach(ProdutoModel entity)
        {
            try
            {
                // Anexar a entidade e marcar como modificada
                _dbSet.Attach(entity);
                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Tratar conflito de concorrência
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();

                if (databaseValues == null)
                    throw new Exception("Produto não encontrado no banco de dados");

                // Resolver conflito (usando valores da database ou do cliente)
                entry.OriginalValues.SetValues(databaseValues);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(ProdutoModel entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(Expression<Func<ProdutoModel, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<ProdutoModel, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        public async Task<IEnumerable<ProdutoModel>> GetByCategoriaIdAsync(int categoriaId)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Where(p => p.Id_Categoria == categoriaId)
                .OrderBy(p => p.Nome_Produto)
                .ToListAsync();
        }
    }
}