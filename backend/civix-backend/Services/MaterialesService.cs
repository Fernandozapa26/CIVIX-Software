
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.DTOs.Materiales;
using civix_backend.Models;

namespace civix_backend.Services
{
    public class MaterialesService
    {
        private readonly CivixDbContext _context;

        public MaterialesService(CivixDbContext context)
        {
            _context = context;
        }

        public async Task<Material> GuardarMaterialAsync(MaterialesDTO dto)
        {
            var material = new Material
            {
                Nombre = dto.Nombre,
                UnidadMedida = dto.UnidadMedida,
                Descripcion = dto.Descripcion
            };

            _context.Materiales.Add(material);

            await _context.SaveChangesAsync();

            return material;
        }

        public async Task<List<Material>> ObtenerMaterialesAsync()
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_context.Materiales);
        }
    }
}
