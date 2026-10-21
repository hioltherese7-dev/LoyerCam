using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Loyercam.Pages;

public partial class LoyersPage : ContentPage
{
    private bool _enregistrementEnCours;

    public LoyersPage()
    {
        InitializeComponent();

        AnneeEntry.Text = DateTime.Today.Year.ToString();
        DatePaiementPicker.Date = DateTime.Today;

        LocatairePicker.SelectedIndexChanged += LocatairePicker_Changed;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await BaseDeDonnees.Instance.InitialiserAsync();
            await ChargerLocatairesAsync();
            await ChargerHistoriqueAsync();
        }
        catch (Exception ex)
        {
            AfficherMessage($"Erreur de chargement : {ex.Message}", true);
        }
    }

    private async Task ChargerLocatairesAsync()
    {
        var locataires =
            await BaseDeDonnees.Instance.ObtenirLocatairesAsync();

        LocatairePicker.ItemsSource = locataires;

        if (locataires.Count == 0)
        {
            AfficherMessage(
                "Enregistrez d'abord un locataire et son logement.",
                true);
        }
    }

    private async void LocatairePicker_Changed(object? sender, EventArgs e)
    {
        if (LocatairePicker.SelectedItem is not Loyercam.Locataire locataire)
        {
            LogementLabel.Text = "Logement : non sélectionné";
            LoyerMensuelLabel.Text = "Loyer mensuel : —";
            return;
        }

        try
        {
            var logement = await BaseDeDonnees.Instance
                .ObtenirLogementParIdAsync(locataire.LogementId);

            if (logement is null)
            {
                LogementLabel.Text = "Logement introuvable";
                LoyerMensuelLabel.Text = "Loyer mensuel : —";
                return;
            }

            LogementLabel.Text =
                $"Logement : {logement.Nom} — {logement.Adresse}";

            LoyerMensuelLabel.Text =
                $"Loyer mensuel : {logement.LoyerMensuel:N0} FCFA";
        }
        catch (Exception ex)
        {
            AfficherMessage($"Erreur : {ex.Message}", true);
        }
    }

    private async void EnregistrerPaiement_Clicked(object? sender, EventArgs e)
    {
        if (_enregistrementEnCours)
            return;

        _enregistrementEnCours = true;

        try
        {
            if (LocatairePicker.SelectedItem is not Loyercam.Locataire locataire)
            {
                AfficherMessage("Choisissez un locataire.", true);
                return;
            }

            string texteMontant = MontantEntry.Text?.Trim() ?? "";

            if (!decimal.TryParse(
                    texteMontant,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal montant)
                && !decimal.TryParse(
                    texteMontant,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out montant))
            {
                AfficherMessage("Saisissez un montant valide.", true);
                return;
            }

            if (montant <= 0)
            {
                AfficherMessage(
                    "Le montant doit être supérieur à zéro.",
                    true);
                return;
            }

            if (MoisPicker.SelectedIndex < 0)
            {
                AfficherMessage("Sélectionnez le mois concerné.", true);
                return;
            }

            if (ModePaiementPicker.SelectedItem is null)
            {
                AfficherMessage("Sélectionnez le mode de paiement.", true);
                return;
            }

            if (!int.TryParse(AnneeEntry.Text, out int annee)
                || annee < 2000
                || annee > 2100)
            {
                AfficherMessage("Saisissez une année valide.", true);
                return;
            }

            int numeroMois = MoisPicker.SelectedIndex + 1;
            string periode = $"{annee:D4}-{numeroMois:D2}";

            var logement = await BaseDeDonnees.Instance
                .ObtenirLogementParIdAsync(locataire.LogementId);

            if (logement is null)
            {
                AfficherMessage(
                    "Le logement associé à ce locataire est introuvable.",
                    true);
                return;
            }

            await BaseDeDonnees.Instance.InitialiserAsync();

            var loyer = await BaseDeDonnees.Instance
                .ObtenirLoyerPourPeriodeAsync(locataire.Id, periode);

            if (loyer is null)
            {
                loyer = new Loyercam.Loyer
                {
                    LocataireId = locataire.Id,
                    Periode = periode,
                    MontantDu = logement.LoyerMensuel,
                    MontantPaye = 0,
                    DateEcheance = new DateTime(annee, numeroMois, 1)
                };

                await BaseDeDonnees.Instance.AjouterLoyerAsync(loyer);

                // Recharger le loyer pour récupérer son identifiant SQLite.
                loyer = await BaseDeDonnees.Instance
                    .ObtenirLoyerPourPeriodeAsync(locataire.Id, periode);

                if (loyer is null)
                {
                    AfficherMessage(
                        "Impossible de retrouver le loyer créé.",
                        true);
                    return;
                }
            }

            decimal reste = Math.Max(
                0, loyer.MontantDu - loyer.MontantPaye);

            if (reste <= 0)
            {
                AfficherMessage(
                    "Ce loyer est déjà entièrement payé.",
                    true);
                return;
            }

            if (montant > reste)
            {
                AfficherMessage(
                    $"Le versement dépasse le solde restant de {reste:N0} FCFA.",
                    true);
                return;
            }

            var paiement = new Loyercam.Paiement
            {
                LoyerId = loyer.Id,
                Montant = montant,
                DatePaiement = DatePaiementPicker.Date ?? DateTime.Today,
                ModePaiement =
                    ModePaiementPicker.SelectedItem.ToString() ?? "Espèces"
            };

            await BaseDeDonnees.Instance.AjouterPaiementAsync(paiement);

            MontantEntry.Text = string.Empty;
            LocatairePicker.SelectedItem = null;
            MoisPicker.SelectedIndex = -1;
            ModePaiementPicker.SelectedIndex = -1;
            DatePaiementPicker.Date = DateTime.Today;

            LogementLabel.Text = "Logement : non sélectionné";
            LoyerMensuelLabel.Text = "Loyer mensuel : —";

            await ChargerHistoriqueAsync();

            AfficherMessage(
                "Paiement enregistré avec succès.",
                false);
        }
        catch (Exception ex)
        {
            AfficherMessage(
                $"Erreur d'enregistrement : {ex.Message}",
                true);
        }
        finally
        {
            _enregistrementEnCours = false;
        }
    }

    private async Task ChargerHistoriqueAsync()
    {
        var paiements =
            await BaseDeDonnees.Instance.ObtenirPaiementsAsync();

        var loyers =
            await BaseDeDonnees.Instance.ObtenirLoyersAsync();

        var locataires =
            await BaseDeDonnees.Instance.ObtenirLocatairesAsync();

        var logements =
            await BaseDeDonnees.Instance.ObtenirLogementsAsync();

        ListePaiements.Children.Clear();

        TotalLabel.Text =
            $"Total encaissé : {paiements.Sum(p => p.Montant):N0} FCFA";

        if (paiements.Count == 0)
        {
            ListePaiements.Children.Add(new Label
            {
                Text = "Aucun paiement enregistré.",
                TextColor = Colors.Gray
            });

            return;
        }

        foreach (var paiement in paiements.OrderByDescending(
                     p => p.DatePaiement))
        {
            var loyer = loyers.FirstOrDefault(
                l => l.Id == paiement.LoyerId);

            var locataire = loyer is null
                ? null
                : locataires.FirstOrDefault(
                    l => l.Id == loyer.LocataireId);

            var logement = locataire is null
                ? null
                : logements.FirstOrDefault(
                    l => l.Id == locataire.LogementId);

            string periode = loyer?.Periode ?? "Période inconnue";

            string solde = loyer is null
                ? "Indisponible"
                : $"{Math.Max(0, loyer.MontantDu - loyer.MontantPaye):N0} FCFA";

            var contenu = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = $"{locataire?.NomComplet ?? "Locataire inconnu"} — {paiement.Montant:N0} FCFA",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = $"Logement : {logement?.Nom ?? "Introuvable"}"
                    },
                    new Label
                    {
                        Text = $"Période : {periode}"
                    },
                    new Label
                    {
                        Text = $"Date du paiement : {paiement.DatePaiement:dd/MM/yyyy}"
                    },
                    new Label
                    {
                        Text = $"Mode : {paiement.ModePaiement}"
                    },
                    new Label
                    {
                        Text = $"Solde restant pour cette période : {solde}"
                    }
                }
            };

            ListePaiements.Children.Add(new Border
            {
                Padding = 14,
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = 10
                    },
                Content = contenu
            });
        }
    }

    private void AfficherMessage(string message, bool erreur)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor =
            erreur ? Colors.Red : Colors.Green;
    }
}