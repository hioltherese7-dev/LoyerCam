
using SQLite;

namespace Loyercam;

public class Loyer
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Locataire concerné
    public int LocataireId { get; set; }

    // Période concernée, par exemple 2026-10
    public string Periode { get; set; } = "";

    // Montant exigible pour cette période
    public decimal MontantDu { get; set; }

    // Total des paiements enregistrés
    public decimal MontantPaye { get; set; }

    // Date à laquelle le loyer est exigible
    public DateTime DateEcheance { get; set; }

    // Solde restant dû
    [Ignore]
    public decimal ResteDu => Math.Max(0, MontantDu - MontantPaye);
}
