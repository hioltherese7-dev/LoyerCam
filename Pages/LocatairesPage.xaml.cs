
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Loyercam.Pages;

public partial class LocatairesPage : ContentPage
{
    private bool _chargementEnCours;

    public LocatairesPage()
    {
        InitializeComponent();

        DateDebutLocationPicker.MaximumDate = DateTime.Today;
        DateDebutLocationPicker.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_chargementEnCours)
            return;

        _chargementEnCours = true;

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
        finally
        {
            _chargementEnCours = false;
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

        NombreLocatairesLabel.Text = locataires.Count == 1
            ? "1 locataire"
            : $"{locataires.Count} locataires";

        if (locataires.Count == 0)
        {
            ListeLocataires.Children.Add(new Label
            {
                Text = "Aucun locataire pour le moment. Utilisez le formulaire ci-dessus pour enregistrer votre premier locataire.",
                FontSize = 14,
                TextColor = Color.FromArgb("#718096"),
                Margin = new Thickness(2, 8)
            });

            return;
        }

        foreach (var locataire in locataires)
        {
            var logement = DonneesApplication.Logements
                .FirstOrDefault(l => l.Id == locataire.LogementId);

            ListeLocataires.Children.Add(
                CreerCarteLocataire(locataire, logement));
        }
    }

    private Border CreerCarteLocataire(
        Locataire locataire, Logement? logement)
    {
        var contenu = new VerticalStackLayout
        {
            Spacing = 13
        };

        // En-tête de la carte
        var entete = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };

        var initiales = ObtenirInitiales(locataire.NomComplet);

        var avatar = new Border
        {
            WidthRequest = 48,
            HeightRequest = 48,
            Padding = 0,
            BackgroundColor = Color.FromArgb("#EAF1FF"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes
                .RoundRectangle
            {
                CornerRadius = 15
            },
            Content = new Label
            {
                Text = initiales,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2563EB"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        var identite = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center
        };

        identite.Children.Add(new Label
        {
            Text = locataire.NomComplet,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#172B4D")
        });

        identite.Children.Add(new Label
        {
            Text = $"Depuis le {locataire.DateDebutLocation:dd/MM/yyyy}",
            FontSize = 12,
            TextColor = Color.FromArgb("#718096")
        });

        entete.Children.Add(avatar);
        entete.Children.Add(identite);
        Grid.SetColumn(identite, 1);

        contenu.Children.Add(entete);

        // Coordonnées du locataire
        contenu.Children.Add(CreerLigneInfo(
            "Téléphone",
            string.IsNullOrWhiteSpace(locataire.Telephone)
                ? "Non renseigné"
                : locataire.Telephone));

        contenu.Children.Add(CreerLigneInfo(
            "Pièce d'identité",
            string.IsNullOrWhiteSpace(locataire.PieceIdentite)
                ? "Non renseignée"
                : locataire.PieceIdentite));

        contenu.Children.Add(CreerLigneInfo(
            "E-mail",
            string.IsNullOrWhiteSpace(locataire.Email)
                ? "Non renseigné"
                : locataire.Email));

        contenu.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#E8EDF5")
        });

        // Logement associé
        contenu.Children.Add(new Label
        {
            Text = "LOGEMENT OCCUPÉ",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#718096")
        });

        contenu.Children.Add(new Label
        {
            Text = logement?.Nom ?? "Logement introuvable",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#172B4D")
        });

        contenu.Children.Add(CreerLigneInfo(
            "Adresse",
            logement?.Adresse ?? "Non disponible"));

        // Loyer mensuel
        var loyerCarte = new Border
        {
            Padding = new Thickness(12, 10),
            BackgroundColor = Color.FromArgb("#F0F7FF"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes
                .RoundRectangle
            {
                CornerRadius = 12
            }
        };

        var loyerContenu = new VerticalStackLayout
        {
            Spacing = 4
        };

        loyerContenu.Children.Add(new Label
        {
            Text = "LOYER MENSUEL",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#64748B")
        });

        loyerContenu.Children.Add(new Label
        {
            Text = logement is null
                ? "Non disponible"
                : $"{logement.LoyerMensuel:N0} FCFA",
            FontSize = 19,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#2563EB")
        });

        loyerCarte.Content = loyerContenu;
        contenu.Children.Add(loyerCarte);

        return new Border
        {
            Padding = new Thickness(16),
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E1E8F2"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes
                .RoundRectangle
            {
                CornerRadius = 18
            },
            Content = contenu
        };
    }

    private static Label CreerLigneInfo(
        string titre, string valeur)
    {
        return new Label
        {
            Text = $"{titre} : {valeur}",
            FontSize = 13,
            TextColor = Color.FromArgb("#64748B")
        };
    }

    private static string ObtenirInitiales(string? nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
            return "LO";

        string[] parties = nom.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (parties.Length == 1)
        {
            return parties[0][..1].ToUpperInvariant();
        }

        return (
            parties[0][..1] +
            parties[^1][..1]
        ).ToUpperInvariant();
    }

    private async void AjouterLocataire_Clicked(
        object? sender, EventArgs e)
    {
        string nom = NomLocataireEntry.Text?.Trim() ?? "";
        string telephone = TelephoneEntry.Text?.Trim() ?? "";
        string piece = PieceEntry.Text?.Trim() ?? "";
        string email = EmailEntry.Text?.Trim() ?? "";

        // Vérifier la date sélectionnée.
        DateTime? dateSelectionnee = DateDebutLocationPicker.Date;

        if (!dateSelectionnee.HasValue)
        {
            AfficherMessage(
                "Sélectionnez une date de début de location.", true);
            return;
        }

        DateTime dateDebut = dateSelectionnee.Value.Date;

        // Vérifier les champs obligatoires.
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
                "Sélectionnez un logement dans la liste.", true);
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

            // Réinitialiser le formulaire.
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

        MessageLocataireLabel.TextColor = erreur
            ? Color.FromArgb("#DC2626")
            : Color.FromArgb("#16803D");
    }
}
