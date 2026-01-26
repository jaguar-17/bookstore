using CloudinaryDotNet.Actions;

namespace BookStore.Interfaces
{
    public interface IPhotoService
    {
        // Método para subir foto (devuelve el resultado de Cloudinary)
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);

        // Método para borrar foto (usando el publicId)
        Task<DeletionResult> DeletePhotoAsync(string publicId);
    }
}
