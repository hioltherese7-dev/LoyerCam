
using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Storage;

namespace Loyercam;

public class BaseDeDonnees
{
    private readonly SQLiteAsyncConnection _database;

    public static BaseDeDonnees Instance { get; } = new();

    private BaseDeDonnees()
    {
        string dossier = ObtenirDossierBaseDeDonnees();
        string chemin = Path.Combine(dossier, "LoyerCam.db3");

        _database = new SQLiteAsyncConnection(chemin);
    }

    private static string ObtenirDossierBaseDeDonnees()
    {
        return FileSystem.Current.AppDataDirectory;
    }

    // =========================
    // INITIALISATION ET MIGRATION
    // =========================

    public async Task InitialiserAsync()
    {
        await _database.CreateTableAsync<Logement>();
        await _database.CreateTableAsync<Locataire>();
        await _database.CreateTableAsync<Loyer>();
        await _database.CreateTableAsync<Paiement>();

        await AjouterColonneDateDebutLocationAsync();
    }

    private async Task AjouterColonneDateDebutLocationAsync()
    {
        var colonnes =
            await _database.GetTableInfoAsync("Locataire");

        bool colonneExiste = colonnes.Any(
            c => c.Name == "DateDebutLocation");

        if (!colonneExiste)
        {
            // Les anciens locataires recevront provisoirement
            // la date du jour. Il faudra corriger leur date réelle.
            long dateParDefaut = DateTime.Today.Ticks;

            await _database.ExecuteAsync(
                "ALTER TABLE Locataire " +
                "ADD COLUMN DateDebutLocation INTEGER NOT NULL " +
                $"DEFAULT {dateParDefaut}");
        }
    }

    // =========================
    // LOGEMENTS
    // =========================

    public Task<int> AjouterLogementAsync(Logement logement)
        => _database.InsertAsync(logement);

    public Task<List<Logement>> ObtenirLogementsAsync()
        => _database.Table<Logement>().ToListAsync();

    public async Task<Logement?> ObtenirLogementParIdAsync(int id)
    {
        return await _database
            .Table<Logement>()
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync();
    }

    // =========================
    // LOCATAIRES
    // =========================

    public Task<int> AjouterLocataireAsync(Locataire locataire)
        => _database.InsertAsync(locataire);

    public Task<List<Locataire>> ObtenirLocatairesAsync()
        => _database.Table<Locataire>().ToListAsync();

    // =========================
    // LOYERS
    // =========================

    public Task<int> AjouterLoyerAsync(Loyer loyer)
        => _database.InsertAsync(loyer);

    public Task<List<Loyer>> ObtenirLoyersAsync()
        => _database.Table<Loyer>().ToListAsync();

    public async Task<Loyer?> ObtenirLoyerParIdAsync(int id)
    {
        return await _database
            .Table<Loyer>()
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<Loyer?> ObtenirLoyerPourPeriodeAsync(
        int locataireId,
        string periode)
    {
        return await _database
            .Table<Loyer>()
            .Where(l => l.LocataireId == locataireId
                     && l.Periode == periode)
            .FirstOrDefaultAsync();
    }

    // =========================
    // PAIEMENTS
    // =========================

    public async Task<int> AjouterPaiementAsync(Paiement paiement)
    {
        if (paiement.Montant <= 0)
        {
            throw new ArgumentException(
                "Le montant du paiement doit être positif.");
        }

        Loyer? loyer =
            await ObtenirLoyerParIdAsync(paiement.LoyerId);

        if (loyer is null)
        {
            throw new InvalidOperationException(
                "Le loyer sélectionné n'existe pas.");
        }

        decimal reste = Math.Max(
            0, loyer.MontantDu - loyer.MontantPaye);

        if (paiement.Montant > reste)
        {
            throw new InvalidOperationException(
                "Le paiement dépasse le montant restant dû.");
        }

        int resultat = await _database.InsertAsync(paiement);

        loyer.MontantPaye += paiement.Montant;
        await _database.UpdateAsync(loyer);

        return resultat;
    }

    public Task<List<Paiement>> ObtenirPaiementsAsync()
        => _database.Table<Paiement>().ToListAsync();

    public async Task<List<Paiement>> ObtenirPaiementsDuLoyerAsync(
        int loyerId)
    {
        return await _database
            .Table<Paiement>()
            .Where(p => p.LoyerId == loyerId)
            .ToListAsync();
    }
}
