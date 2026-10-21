
using SQLite;
using System;

namespace Loyercam;

public class Locataire
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string NomComplet { get; set; } = "";

    public string Telephone { get; set; } = "";

    public string PieceIdentite { get; set; } = "";

    public string Email { get; set; } = "";

    public int LogementId { get; set; }

    public DateTime DateDebutLocation { get; set; } = DateTime.Today;
}
