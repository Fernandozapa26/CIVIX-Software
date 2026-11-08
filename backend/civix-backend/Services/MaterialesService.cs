
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


        //este endpoint permite consultar los materiales de la tabla
        public async Task<List<Material>> ObtenerMaterialesAsync()
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_context.Materiales);
        }

        //Este endpoint permitirá consultar el material por su identificador id
        public async Task<Material?> ObtenerMaterialPorIdAsync(int id)
        {
            return await _context.Materiales.FindAsync(id);
        }

        //Este endpoint permitiria actualizar materiales
        public async Task<Material?> ActualizarMaterialAsync(int id, MaterialesDTO dto)
        {
            var material = await _context.Materiales.FindAsync(id);

            if (material == null)
            {
                return null;
            }

            material.Nombre = dto.Nombre;
            material.UnidadMedida = dto.UnidadMedida;
            material.Descripcion = dto.Descripcion;

            await _context.SaveChangesAsync();

            return material;
        }

        // Este endpoint permite cambiar el estatado de activo a inactivo
        public async Task<bool> DesactivarMaterialAsync(int id)
        {
            var material = await _context.Materiales.FindAsync(id);

            if (material == null || !material.Activo)
            {
                return false;
            }

            material.Activo = false;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
