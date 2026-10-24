
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Loyercam.Pages;

public partial class ImpayesPage : ContentPage
{
    private readonly BaseDeDonnees _baseDeDonnees =
        BaseDeDonnees.Instance;

    private readonly ObservableCollection<ImpayesItem> _impayes = new();

    private bool _chargementEnCours;

    public ImpayesPage()
    {
        InitializeComponent();

        ListeImpayes.ItemsSource = _impayes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await ChargerImpayesAsync();
    }

    private async void ActualiserButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await ChargerImpayesAsync();
    }

    private async Task ChargerImpayesAsync()
    {
        if (_chargementEnCours)
            return;

        _chargementEnCours = true;
        ActualiserButton.IsEnabled = false;

        try
        {
            await _baseDeDonnees.InitialiserAsync();

            var logements =
                await _baseDeDonnees.ObtenirLogementsAsync();

            var locataires =
                await _baseDeDonnees.ObtenirLocatairesAsync();

            var loyers =
                await _baseDeDonnees.ObtenirLoyersAsync();

            DateTime aujourdHui = DateTime.Today;

            DateTime debutMoisActuel = new(
                aujourdHui.Year,
                aujourdHui.Month,
                1);

            // Créer les loyers manquants des mois précédents.
            // Le mois en cours n'est pas généré automatiquement.
            foreach (var locataire in locataires)
            {
                if (locataire.DateDebutLocation == default)
                    continue;

                var logement = logements.FirstOrDefault(
                    l => l.Id == locataire.LogementId);

                if (logement is null ||
                    logement.LoyerMensuel <= 0)
                {
                    continue;
                }

                DateTime premierMois = new(
                    locataire.DateDebutLocation.Year,
                    locataire.DateDebutLocation.Month,
                    1);

                for (DateTime mois = premierMois;
                     mois < debutMoisActuel;
                     mois = mois.AddMonths(1))
                {
                    string periode = mois.ToString(
                        "MM/yyyy",
                        CultureInfo.InvariantCulture);

                    bool existe = loyers.Any(l =>
                        l.LocataireId == locataire.Id &&
                        l.Periode == periode);

                    if (existe)
                        continue;

                    var nouveauLoyer = new Loyer
                    {
                        LocataireId = locataire.Id,
                        Periode = periode,
                        MontantDu = logement.LoyerMensuel,
                        MontantPaye = 0,
                        DateEcheance = mois
                    };

                    await _baseDeDonnees.AjouterLoyerAsync(
                        nouveauLoyer);

                    // Empêcher la création en double
                    // durant le chargement actuel.
                    loyers.Add(nouveauLoyer);
                }
            }

            _impayes.Clear();

            decimal totalImpayes = 0;
            int nombreImpayes = 0;

            var loyersEchus = loyers
                .Where(l =>
                    l.DateEcheance.Date <= aujourdHui)
                .OrderBy(l => l.DateEcheance)
                .ToList();

            foreach (var loyer in loyersEchus)
            {
                decimal reste = Math.Max(
                    0,
                    loyer.MontantDu - loyer.MontantPaye);

                // Ne pas afficher les loyers entièrement payés.
                if (reste <= 0)
                    continue;

                var locataire = locataires.FirstOrDefault(
                    l => l.Id == loyer.LocataireId);

                if (locataire is null)
                    continue;

                var logement = logements.FirstOrDefault(
                    l => l.Id == locataire.LogementId);

                _impayes.Add(new ImpayesItem
                {
                    NomLocataire = locataire.NomComplet,

                    NomLogement = logement?.Nom
                        ?? "Logement non trouvé",

                    Periode = loyer.Periode,

                    EcheanceTexte = loyer.DateEcheance
                        .ToString("dd/MM/yyyy"),

                    MontantDu = loyer.MontantDu,
                    MontantPaye = loyer.MontantPaye,
                    ResteDu = reste
                });

                totalImpayes += reste;
                nombreImpayes++;
            }

            TotalImpayesLabel.Text =
                $"{totalImpayes:N0} FCFA";

            NombreImpayesLabel.Text =
                nombreImpayes == 1
                    ? "1 loyer en retard"
                    : $"{nombreImpayes} loyers en retard";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erreur",
                $"Impossible de charger les impayés : {ex.Message}",
                "OK");
        }
    
        finally
        {
            ActualiserButton.IsEnabled = true;
            _chargementEnCours = false;
        }
    }
}

public class ImpayesItem
{
    public string NomLocataire { get; set; } = "";

    public string NomLogement { get; set; } = "";

    public string Periode { get; set; } = "";

    public string EcheanceTexte { get; set; } = "";

    public decimal MontantDu { get; set; }

    public decimal MontantPaye { get; set; }

    public decimal ResteDu { get; set; }

    public string MontantDuTexte =>
        $"{MontantDu:N0} FCFA";

    public string MontantPayeTexte =>
        $"{MontantPaye:N0} FCFA";

    public string ResteDuTexte =>
        $"{ResteDu:N0} FCFA";
}
