
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Loyercam.Pages;

public partial class LogementsPage : ContentPage
{
    private bool _chargementEnCours;
    private Logement? _logementEnCoursModification;

    public LogementsPage()
    {
        InitializeComponent();
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
                Text = "Aucun logement pour le moment. Ajoutez votre premier bien avec le formulaire ci-dessus.",
                FontSize = 14,
                TextColor = Color.FromArgb("#718096"),
                Margin = new Thickness(2, 8)
            });
        }
        else
        {
            foreach (var logement in logements)
            {
                ListeLogements.Children.Add(CreerCarte(logement));
            }
        }

        NombreLogementsLabel.Text = logements.Count == 1
            ? "1 logement"
            : $"{logements.Count} logements";
    }

    private async void EnregistrerLogement_Clicked(
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

        string type =
            TypePicker.SelectedItem?.ToString() ?? "Non précisé";

        try
        {
            await BaseDeDonnees.Instance.InitialiserAsync();

            if (_logementEnCoursModification is not null)
            {
                _logementEnCoursModification.Nom = nom;
                _logementEnCoursModification.Adresse = adresse;
                _logementEnCoursModification.Type = type;
                _logementEnCoursModification.LoyerMensuel = loyer;
                _logementEnCoursModification.NombreChambres = chambres;

                await BaseDeDonnees.Instance.ModifierLogementAsync(
                    _logementEnCoursModification);

                ReinitialiserFormulaire();
                await ChargerLogementsAsync();

                AfficherMessage(
                    "Les modifications ont été enregistrées.", false);
            }
            else
            {
                var logement = new Logement
                {
                    Nom = nom,
                    Adresse = adresse,
                    Type = type,
                    LoyerMensuel = loyer,
                    NombreChambres = chambres
                };

                await BaseDeDonnees.Instance
                    .AjouterLogementAsync(logement);

                ReinitialiserFormulaire();
                await ChargerLogementsAsync();

                AfficherMessage(
                    "Logement enregistré dans la base de données.",
                    false);
            }
        }
        catch (Exception ex)
        {
            AfficherMessage(
                $"Erreur d'enregistrement : {ex.Message}", true);
        }
    }

    private Border CreerCarte(Logement logement)
    {
        var contenu = new VerticalStackLayout
        {
            Spacing = 14
        };

        // En-tête : nom et type du logement
        var entete = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };

        var nomLabel = new Label
        {
            Text = logement.Nom,
            FontSize = 19,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#172B4D"),
            VerticalOptions = LayoutOptions.Center
        };

        var typeBadge = new Border
        {
            Padding = new Thickness(9, 6),
            BackgroundColor = Color.FromArgb("#EAF1FF"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes
                .RoundRectangle
            {
                CornerRadius = 9
            },
            Content = new Label
            {
                Text = logement.Type,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2563EB"),
                VerticalOptions = LayoutOptions.Center
            }
        };

        entete.Children.Add(nomLabel);
        entete.Children.Add(typeBadge);
        Grid.SetColumn(typeBadge, 1);

        contenu.Children.Add(entete);

        // Adresse du logement
        var adresseLigne = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center
        };

        adresseLigne.Children.Add(new Label
        {
            Text = "⌖",
            FontSize = 18,
            TextColor = Color.FromArgb("#64748B")
        });

        adresseLigne.Children.Add(new Label
        {
            Text = logement.Adresse,
            FontSize = 13,
            TextColor = Color.FromArgb("#64748B"),
            VerticalOptions = LayoutOptions.Center
        });

        contenu.Children.Add(adresseLigne);

        // Séparateur
        contenu.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#E8EDF5")
        });

        // Loyer et nombre de chambres
        var informations = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };

        var loyerBloc = new VerticalStackLayout
        {
            Spacing = 5
        };

        loyerBloc.Children.Add(new Label
        {
            Text = "LOYER MENSUEL",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#718096")
        });

        loyerBloc.Children.Add(new Label
        {
            Text = $"{logement.LoyerMensuel:N0} FCFA",
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#2563EB")
        });

        var chambresBloc = new VerticalStackLayout
        {
            Spacing = 5
        };

        chambresBloc.Children.Add(new Label
        {
            Text = "CHAMBRES",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#718096")
        });

        chambresBloc.Children.Add(new Label
        {
            Text = logement.NombreChambres.ToString(),
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#172B4D")
        });

        informations.Children.Add(loyerBloc);
        informations.Children.Add(chambresBloc);
        Grid.SetColumn(chambresBloc, 1);

        contenu.Children.Add(informations);

        // Boutons Modifier et Supprimer
        var boutons = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };

        var modifierButton = new Button
        {
            Text = "Modifier",
            BackgroundColor = Color.FromArgb("#EAF1FF"),
            TextColor = Color.FromArgb("#2563EB"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            CornerRadius = 10,
            HeightRequest = 44,
            Padding = new Thickness(8, 0)
        };

        modifierButton.Clicked +=
            (_, _) => CommencerModification(logement);

        var supprimerButton = new Button
        {
            Text = "Supprimer",
            BackgroundColor = Color.FromArgb("#FFF0F0"),
            TextColor = Color.FromArgb("#DC2626"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            CornerRadius = 10,
            HeightRequest = 44,
            Padding = new Thickness(8, 0)
        };

        supprimerButton.Clicked += async (_, _) =>
            await SupprimerLogementAsync(logement);

        boutons.Children.Add(modifierButton);
        boutons.Children.Add(supprimerButton);
        Grid.SetColumn(supprimerButton, 1);

        contenu.Children.Add(boutons);

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

    private void CommencerModification(Logement logement)
    {
        _logementEnCoursModification = logement;

        NomEntry.Text = logement.Nom;
        AdresseEntry.Text = logement.Adresse;

        LoyerEntry.Text = logement.LoyerMensuel
            .ToString(CultureInfo.InvariantCulture);

        ChambresEntry.Text =
            logement.NombreChambres.ToString();

        TypePicker.SelectedItem =
            TypePicker.Items.Contains(logement.Type)
                ? logement.Type
                : null;

        FormulaireTitreLabel.Text = "Modifier le logement";
        EnregistrerButton.Text = "Enregistrer les modifications";

        AnnulerModificationButton.IsVisible = true;
        AnnulerModificationButton.IsEnabled = true;

        AfficherMessage(
            "Modifiez les informations, puis enregistrez.",
            false);
    }

    private void AnnulerModification_Clicked(
        object? sender, EventArgs e)
    {
        ReinitialiserFormulaire();
        AfficherMessage("Modification annulée.", false);
    }

    private void ReinitialiserFormulaire()
    {
        _logementEnCoursModification = null;

        NomEntry.Text = string.Empty;
        AdresseEntry.Text = string.Empty;
        LoyerEntry.Text = string.Empty;
        ChambresEntry.Text = string.Empty;
        TypePicker.SelectedItem = null;

        FormulaireTitreLabel.Text = "Nouveau logement";
        EnregistrerButton.Text = "＋  Ajouter le logement";

        AnnulerModificationButton.IsVisible = false;
        AnnulerModificationButton.IsEnabled = false;
    }

    private async Task SupprimerLogementAsync(Logement logement)
    {
        bool confirmer = await DisplayAlertAsync(
            "Confirmer la suppression",
            $"Voulez-vous vraiment supprimer le logement « {logement.Nom} » ?",
            "Supprimer",
            "Annuler");

        if (!confirmer)
            return;

        try
        {
            await BaseDeDonnees.Instance.InitialiserAsync();

            await BaseDeDonnees.Instance
                .SupprimerLogementAsync(logement.Id);

            if (_logementEnCoursModification?.Id == logement.Id)
                ReinitialiserFormulaire();

            await ChargerLogementsAsync();

            AfficherMessage("Le logement a été supprimé.", false);
        }
        catch (Exception ex)
        {
            AfficherMessage(ex.Message, true);

            await DisplayAlertAsync(
                "Suppression impossible",
                ex.Message,
                "OK");
        }
    }

    private void AfficherMessage(string message, bool erreur)
    {
        MessageLabel.Text = message;

        MessageLabel.TextColor = erreur
            ? Color.FromArgb("#DC2626")
            : Color.FromArgb("#16803D");
    }
}
