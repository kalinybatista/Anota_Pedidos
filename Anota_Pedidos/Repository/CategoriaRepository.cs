using Anota_Pedidos.Data;
using Anota_Pedidos.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Anota_Pedidos.Repository
{
    public class CategoriaRepository : Repository<CategoriaModel>
    {
        public CategoriaRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CategoriaModel>> GetAllWithProductsAsync()
        {
            return await _dbSet
                .Include(c => c.Produtos)
                .OrderBy(c => c.Nome_Categoria)
                .ToListAsync();
        }

        public async Task<CategoriaModel?> GetByIdWithProductsAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Produtos)
                .FirstOrDefaultAsync(c => c.Id_Categoria == id);
        }

        // Método alternativo para buscar com produtos
        public async Task<CategoriaModel?> GetByIdWithProductsAsync(int id, bool includeProducts = true)
        {
            var query = _dbSet.AsQueryable();

            if (includeProducts)
            {
                query = query.Include(c => c.Produtos);
            }

            return await query.FirstOrDefaultAsync(c => c.Id_Categoria == id);
        }

        // 🔥 MÉTODO ADD EXPLÍCITO (SE NÃO EXISTIR NA CLASSE BASE)
        public async Task<CategoriaModel> AddAsync(CategoriaModel entity)
        {
            try
            {
                if (entity == null)
                    throw new Exception("Entidade não pode ser nula");

                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao adicionar categoria: {ex.Message}");
            }
        }

        public async Task UpdateAsync(CategoriaModel entity)
        {
            try
            {
                var existingCategoria = await _dbSet
                    .FirstOrDefaultAsync(c => c.Id_Categoria == entity.Id_Categoria);

                if (existingCategoria == null)
                    throw new Exception($"Categoria com ID {entity.Id_Categoria} não encontrada");

                existingCategoria.Nome_Categoria = entity.Nome_Categoria;
                existingCategoria.Descricao_Categoria = entity.Descricao_Categoria;

                _context.Entry(existingCategoria).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao atualizar categoria: {ex.Message}");
            }
        }
    }
}