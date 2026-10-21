
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Loyercam.Pages;

public partial class LocatairesPage : ContentPage
{
    public LocatairesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await BaseDeDonnees.Instance.InitialiserAsync();
            await ChargerLogementsAsync();
            await ChargerLocatairesAsync();
        }
        catch (Exception ex)
        {
            AfficherMessage(
                $"Erreur de chargement : {ex.Message}", true);
        }
    }

    private async Task ChargerLogementsAsync()
    {
        var logements =
            await BaseDeDonnees.Instance.ObtenirLogementsAsync();

        DonneesApplication.Logements.Clear();
        DonneesApplication.Logements.AddRange(logements);

        LogementPicker.ItemsSource = logements;
    }

    private async Task ChargerLocatairesAsync()
    {
        var locataires =
            await BaseDeDonnees.Instance.ObtenirLocatairesAsync();

        ListeLocataires.Children.Clear();

        if (locataires.Count == 0)
        {
            ListeLocataires.Children.Add(new Label
            {
                Text = "Aucun locataire enregistré.",
                TextColor = Colors.Gray
            });

            return;
        }

        foreach (var locataire in locataires)
        {
            var logement = DonneesApplication.Logements
                .FirstOrDefault(l => l.Id == locataire.LogementId);

            var contenu = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = locataire.NomComplet,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = $"Téléphone : {locataire.Telephone}"
                    },
                    new Label
                    {
                        Text = "Pièce d'identité : " +
                            (string.IsNullOrWhiteSpace(
                                locataire.PieceIdentite)
                                ? "Non renseignée"
                                : locataire.PieceIdentite)
                    },
                    new Label
                    {
                        Text = "E-mail : " +
                            (string.IsNullOrWhiteSpace(locataire.Email)
                                ? "Non renseigné"
                                : locataire.Email)
                    },
                    new Label
                    {
                        Text = $"Début de location : " +
                            $"{locataire.DateDebutLocation:dd/MM/yyyy}"
                    },
                    new Label
                    {
                        Text = $"Logement occupé : " +
                            $"{(logement?.Nom ?? "Logement introuvable")}"
                    },
                    new Label
                    {
                        Text = $"Adresse : " +
                            $"{(logement?.Adresse ?? "Non disponible")}"
                    },
                    new Label
                    {
                        Text = "Loyer mensuel : " +
                            $"{(logement?.LoyerMensuel.ToString("N0") ?? "Non disponible")} FCFA"
                    }
                }
            };

            ListeLocataires.Children.Add(new Border
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

    private async void AjouterLocataire_Clicked(
        object? sender, EventArgs e)
    {
        string nom = NomLocataireEntry.Text?.Trim() ?? "";
        string telephone = TelephoneEntry.Text?.Trim() ?? "";
        string piece = PieceEntry.Text?.Trim() ?? "";
        string email = EmailEntry.Text?.Trim() ?? "";

        // Récupérer la date de début de location.
        DateTime? dateSelectionnee =
            DateDebutLocationPicker.Date;

        if (!dateSelectionnee.HasValue)
        {
            AfficherMessage(
                "Sélectionnez une date de début de location.", true);
            return;
        }

        DateTime dateDebut = dateSelectionnee.Value.Date;

        if (string.IsNullOrWhiteSpace(nom) ||
            string.IsNullOrWhiteSpace(telephone))
        {
            AfficherMessage(
                "Saisissez le nom et le téléphone du locataire.",
                true);
            return;
        }

        if (dateDebut > DateTime.Today)
        {
            AfficherMessage(
                "La date de début ne peut pas être dans le futur.",
                true);
            return;
        }

        if (LogementPicker.SelectedItem is not Logement logement)
        {
            AfficherMessage(
                "Sélectionnez un logement dans la liste.",
                true);
            return;
        }

        var locataire = new Locataire
        {
            NomComplet = nom,
            Telephone = telephone,
            PieceIdentite = piece,
            Email = email,
            LogementId = logement.Id,
            DateDebutLocation = dateDebut
        };

        try
        {
            await BaseDeDonnees.Instance.InitialiserAsync();

            await BaseDeDonnees.Instance.AjouterLocataireAsync(
                locataire);

            await ChargerLocatairesAsync();

            NomLocataireEntry.Text = string.Empty;
            TelephoneEntry.Text = string.Empty;
            PieceEntry.Text = string.Empty;
            EmailEntry.Text = string.Empty;

            LogementPicker.SelectedItem = null;
            DateDebutLocationPicker.Date = DateTime.Today;

            AfficherMessage(
                "Locataire enregistré avec succès.", false);
        }
        catch (Exception ex)
        {
            AfficherMessage(
                $"Erreur d'enregistrement : {ex.Message}", true);
        }
    }

    private void AfficherMessage(string message, bool erreur)
    {
        MessageLocataireLabel.Text = message;
        MessageLocataireLabel.TextColor =
            erreur ? Colors.Red : Colors.Green;
    }
}
