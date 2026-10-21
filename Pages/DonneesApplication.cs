
using SQLite;
using System.Collections.Generic;

namespace Loyercam;

public class Logement
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nom { get; set; } = "";
    public string Adresse { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal LoyerMensuel { get; set; }
    public int NombreChambres { get; set; }
}

public static class DonneesApplication
{
    public static List<Logement> Logements { get; } = new();
}
