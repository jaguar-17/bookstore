namespace BookStore.DTOs
{
    public class PedidoCreacionDto
    {
        public int UsuarioId { get; set; }
        public List<PedidoItemDto> Items { get; set; } = [];
    }
}