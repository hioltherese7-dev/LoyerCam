
using SQLite;

namespace Loyercam;

public class Paiement
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Loyer auquel ce versement correspond
    public int LoyerId { get; set; }

    // Montant réellement versé
    public decimal Montant { get; set; }

    // Date du versement
    public DateTime DatePaiement { get; set; } = DateTime.Now;

    // Moyen de paiement : espèces, virement, etc.
    public string ModePaiement { get; set; } = "Espèces";

    // Référence ou commentaire facultatif
    public string Reference { get; set; } = "";
}
