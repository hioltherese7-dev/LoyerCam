
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Loyercam.Pages;

public partial class LogementsPage : ContentPage
{
    private bool _chargementEnCours;

    public LogementsPage()
    {
        InitializeComponent();
        InitialiserEtChargerAsync();
    }

    private async void InitialiserEtChargerAsync()
    {
        if (_chargementEnCours)
            return;

        _chargementEnCours = true;

        try
        {
            await BaseDeDonnees.Instance.InitialiserAsync();
            await ChargerLogementsAsync();
        }
        catch (Exception ex)
        {
            AfficherMessage(
                $"Erreur lors du chargement : {ex.Message}", true);
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

        ListeLogements.Children.Clear();

        if (logements.Count == 0)
        {
            ListeLogements.Children.Add(new Label
            {
                Text = "Aucun logement ajouté.",
                TextColor = Colors.Gray
            });
        }
        else
        {
            foreach (var logement in logements)
            {
                ListeLogements.Children.Add(CreerCarte(logement));
            }
        }

        NombreLogementsLabel.Text =
            $"Nombre de logements : {logements.Count}";
    }

    private async void AjouterLogement_Clicked(
        object? sender, EventArgs e)
    {
        string nom = NomEntry.Text?.Trim() ?? "";
        string adresse = AdresseEntry.Text?.Trim() ?? "";
        string loyerTexte = LoyerEntry.Text?.Trim() ?? "";
        string chambresTexte = ChambresEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nom) ||
            string.IsNullOrWhiteSpace(adresse))
        {
            AfficherMessage(
                "Saisissez le nom et l'adresse du logement.", true);
            return;
        }

        if (!decimal.TryParse(
                loyerTexte,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal loyer) &&
            !decimal.TryParse(
                loyerTexte,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out loyer))
        {
            AfficherMessage("Saisissez un loyer valide.", true);
            return;
        }

        if (loyer <= 0)
        {
            AfficherMessage(
                "Le loyer doit être supérieur à zéro.", true);
            return;
        }

        int chambres = 0;

        if (!string.IsNullOrWhiteSpace(chambresTexte) &&
            (!int.TryParse(chambresTexte, out chambres) ||
             chambres < 0))
        {
            AfficherMessage(
                "Le nombre de chambres doit être positif ou nul.",
                true);
            return;
        }

        string type = TypePicker.SelectedItem?.ToString()
                      ?? "Non précisé";

        var logement = new Loyercam.Logement
        {
            Nom = nom,
            Adresse = adresse,
            Type = type,
            LoyerMensuel = loyer,
            NombreChambres = chambres
        };

        try
        {
            await BaseDeDonnees.Instance.InitialiserAsync();

            await BaseDeDonnees.Instance.AjouterLogementAsync(
                logement);

            await ChargerLogementsAsync();

            NomEntry.Text = string.Empty;
            AdresseEntry.Text = string.Empty;
            LoyerEntry.Text = string.Empty;
            ChambresEntry.Text = string.Empty;
            TypePicker.SelectedItem = null;

            AfficherMessage(
                "Logement enregistré dans la base de données.",
                false);
        }
        catch (Exception ex)
        {
            AfficherMessage(
                $"Erreur d'enregistrement : {ex.Message}", true);
        }
    }

    private Border CreerCarte(Loyercam.Logement logement)
    {
        var contenu = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = logement.Nom,
                    FontSize = 19,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = $"Adresse : {logement.Adresse}"
                },
                new Label
                {
                    Text = $"Type : {logement.Type}"
                },
                new Label
                {
                    Text = $"Loyer mensuel : {logement.LoyerMensuel:N0} FCFA"
                },
                new Label
                {
                    Text = $"Nombre de chambres : {logement.NombreChambres}"
                }
            }
        };

        return new Border
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
        };
    }

    private void AfficherMessage(string message, bool erreur)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor =
            erreur ? Colors.Red : Colors.Green;
    }
}
