namespace API_Gestor_Minimal.Model
{
    public class Usuario
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public string ApellidoP { get; set; }
        public string ApellidoM { get; set; } = string.Empty;
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public DateTime FechaCreado { get; set; } = System.DateTime.Now;
        public string Rol { get; set; } = "Admin";
    }
}
