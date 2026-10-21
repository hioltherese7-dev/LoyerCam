
using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace Loyercam.Pages;

public partial class AccueilPage : ContentPage
{
    private readonly BaseDeDonnees _baseDeDonnees =
        BaseDeDonnees.Instance;

    private bool _chargementEnCours;

    public AccueilPage()
    {
        InitializeComponent();

        DateLabel.Text = DateTime.Today.ToString("dd/MM/yyyy");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ChargerTableauDeBordAsync();
    }

    private async Task ChargerTableauDeBordAsync()
    {
        if (_chargementEnCours)
            return;

        _chargementEnCours = true;

        try
        {
            await _baseDeDonnees.InitialiserAsync();

            var logements =
                await _baseDeDonnees.ObtenirLogementsAsync();

            var locataires =
                await _baseDeDonnees.ObtenirLocatairesAsync();

            var loyers =
                await _baseDeDonnees.ObtenirLoyersAsync();

            var paiements =
                await _baseDeDonnees.ObtenirPaiementsAsync();

            decimal totalEncaisse = paiements.Sum(p => p.Montant);

            decimal totalImpayes = loyers.Sum(l =>
                Math.Max(0, l.MontantDu - l.MontantPaye));

            LogementsLabel.Text = logements.Count.ToString();
            LocatairesLabel.Text = locataires.Count.ToString();

            EncaisséLabel.Text =
                $"{totalEncaisse:N0} FCFA";

            ImpayesLabel.Text =
                $"{totalImpayes:N0} FCFA";

            MessageLabel.Text = "Données actualisées.";
        }
        catch (Exception ex)
        {
            MessageLabel.Text =
                "Impossible de charger les données.";

            await DisplayAlertAsync(
                "Erreur",
                $"Le tableau de bord n'a pas pu être chargé : {ex.Message}",
                "OK");
        }
        finally
        {
            _chargementEnCours = false;
        }
    }

    private async void ActualiserButton_Clicked(
        object? sender, EventArgs e)
    {
        await ChargerTableauDeBordAsync();
    }

    private async void LogementsButton_Clicked(
        object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//logements");
    }

    private async void LocatairesButton_Clicked(
        object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//locataires");
    }

    private async void LoyersButton_Clicked(
        object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//loyers");
    }

    private async void ImpayesButton_Clicked(
        object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//impayes");
    }

    private async void QuittancesButton_Clicked(
        object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//quittances");
    }
}
