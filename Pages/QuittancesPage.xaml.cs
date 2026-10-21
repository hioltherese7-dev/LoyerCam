
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using CommunityToolkit.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Loyercam.Pages;

public partial class QuittancesPage : ContentPage
{
    private readonly BaseDeDonnees _baseDeDonnees =
        BaseDeDonnees.Instance;

    private readonly ObservableCollection<QuittanceItem> _quittances = new();

    private List<QuittanceItem> _tousLesPaiements = new();

    private bool _chargementEnCours;

    public QuittancesPage()
    {
        InitializeComponent();
        ListeQuittances.ItemsSource = _quittances;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ChargerQuittancesAsync();
    }

    private async void ActualiserButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await ChargerQuittancesAsync();
    }

    private async Task ChargerQuittancesAsync()
    {
        if (_chargementEnCours)
            return;

        _chargementEnCours = true;
        ActualiserButton.IsEnabled = false;

        try
        {
            await _baseDeDonnees.InitialiserAsync();

            var paiements = await _baseDeDonnees.ObtenirPaiementsAsync();
            var loyers = await _baseDeDonnees.ObtenirLoyersAsync();
            var locataires = await _baseDeDonnees.ObtenirLocatairesAsync();
            var logements = await _baseDeDonnees.ObtenirLogementsAsync();

            _tousLesPaiements = paiements
                .OrderByDescending(p => p.DatePaiement)
                .Select(p =>
                {
                    var loyer = loyers.FirstOrDefault(
                        l => l.Id == p.LoyerId);

                    var locataire = loyer is null
                        ? null
                        : locataires.FirstOrDefault(
                            l => l.Id == loyer.LocataireId);

                    var logement = locataire is null
                        ? null
                        : logements.FirstOrDefault(
                            l => l.Id == locataire.LogementId);

                    decimal reste = loyer is null
                        ? 0
                        : Math.Max(
                            0,
                            loyer.MontantDu - loyer.MontantPaye);

                    return new QuittanceItem
                    {
                        NomLocataire = locataire?.NomComplet
                            ?? "Locataire introuvable",

                        NomLogement = logement?.Nom
                            ?? "Logement introuvable",

                        Periode = loyer?.Periode
                            ?? "Période inconnue",

                        Montant = p.Montant,
                        DatePaiement = p.DatePaiement,
                        ModePaiement = p.ModePaiement,

                        Reference = string.IsNullOrWhiteSpace(p.Reference)
                            ? "Non renseignée"
                            : p.Reference,

                        ResteDu = reste
                    };
                })
                .ToList();

            AfficherPaiements(_tousLesPaiements);

            MessageLabel.Text = _tousLesPaiements.Count == 0
                ? "Aucun paiement n'a encore été enregistré."
                : "Historique des paiements chargé.";
        }
        catch (Exception ex)
        {
            MessageLabel.Text = "Erreur lors du chargement.";

            await DisplayAlertAsync(
                "Erreur",
                $"Impossible de charger les paiements : {ex.Message}",
                "OK");
        }
        finally
        {
            ActualiserButton.IsEnabled = true;
            _chargementEnCours = false;
        }
    }

    private void RechercheEntry_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        string recherche = e.NewTextValue?.Trim() ?? "";

        var resultats = string.IsNullOrWhiteSpace(recherche)
            ? _tousLesPaiements
            : _tousLesPaiements
                .Where(p =>
                    p.NomLocataire.Contains(
                        recherche,
                        StringComparison.OrdinalIgnoreCase)
                    || p.NomLogement.Contains(
                        recherche,
                        StringComparison.OrdinalIgnoreCase)
                    || p.Periode.Contains(
                        recherche,
                        StringComparison.OrdinalIgnoreCase)
                    || p.ModePaiement.Contains(
                        recherche,
                        StringComparison.OrdinalIgnoreCase)
                    || p.Reference.Contains(
                        recherche,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        AfficherPaiements(resultats);
    }

    private void AfficherPaiements(
        IEnumerable<QuittanceItem> paiements)
    {
        var liste = paiements.ToList();

        _quittances.Clear();

        foreach (var paiement in liste)
            _quittances.Add(paiement);

        decimal total = liste.Sum(p => p.Montant);

        TotalPaiementsLabel.Text = $"{total:N0} FCFA";

        NombrePaiementsLabel.Text = liste.Count == 1
            ? "1 paiement"
            : $"{liste.Count} paiements";
    }

    private async void EnregistrerPdfButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button bouton ||
            bouton.CommandParameter is not QuittanceItem paiement)
            return;

        try
        {
            byte[] contenu = QuittancePdfService.GenererPdf(paiement);
            string nomFichier = CreerNomFichier(paiement);

            using var flux = new MemoryStream(contenu);

            var resultat = await FileSaver.Default.SaveAsync(
                nomFichier,
                flux,
                CancellationToken.None);

            if (resultat.IsSuccessful)
            {
                await DisplayAlertAsync(
                    "PDF enregistré",
                    "Le document PDF a été enregistré.",
                    "OK");
            }
            else if (resultat.Exception is not null)
            {
                await DisplayAlertAsync(
                    "Erreur",
                    resultat.Exception.Message,
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erreur PDF",
                ex.Message,
                "OK");
        }
    }

    private async void PartagerPdfButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button bouton ||
            bouton.CommandParameter is not QuittanceItem paiement)
            return;

        try
        {
            byte[] contenu = QuittancePdfService.GenererPdf(paiement);
            string chemin = Path.Combine(
                FileSystem.CacheDirectory,
                CreerNomFichier(paiement));

            await File.WriteAllBytesAsync(chemin, contenu);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Partager la quittance de loyer",
                File = new ShareFile(chemin, "application/pdf")
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erreur de partage",
                ex.Message,
                "OK");
        }
    }

    private static string CreerNomFichier(QuittanceItem paiement)
    {
        string locataire = string.Concat(
            paiement.NomLocataire.Split(
                Path.GetInvalidFileNameChars()));

        locataire = locataire.Replace(' ', '_');

        string periode = string.Concat(
            paiement.Periode.Split(
                Path.GetInvalidFileNameChars()));

        return $"Quittance_{locataire}_{periode}.pdf";
    }
}

public class QuittanceItem
{
    public string NomLocataire { get; set; } = "";
    public string NomLogement { get; set; } = "";
    public string Periode { get; set; } = "";
    public decimal Montant { get; set; }
    public DateTime DatePaiement { get; set; }
    public string ModePaiement { get; set; } = "";
    public string Reference { get; set; } = "";
    public decimal ResteDu { get; set; }

    public string MontantTexte => $"{Montant:N0} FCFA";

    public string DatePaiementTexte =>
        DatePaiement.ToString("dd/MM/yyyy");

    public string ReferenceTexte => Reference;

    public string ResteDuTexte => $"{ResteDu:N0} FCFA";
}
