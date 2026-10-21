
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Loyercam.Pages;

public static class QuittancePdfService
{
    private const string NomProprietaire = "DADA NELSON";
    private const string TelephoneProprietaire = "657526945";
    private const string AdresseProprietaire = "Douala, Cameroun";

    public static byte[] GenererPdf(QuittanceItem paiement)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        string titre = paiement.ResteDu <= 0
            ? "QUITTANCE DE LOYER"
            : "REÇU DE PAIEMENT DE LOYER";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header().Column(header =>
                {
                    header.Item()
                        .Text("LOYERCAM")
                        .FontSize(25)
                        .Bold()
                        .FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);

                    header.Item()
                        .Text("Gestion locative")
                        .FontSize(10)
                        .FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(25).Column(column =>
                {
                    column.Spacing(14);

                    column.Item()
                        .AlignCenter()
                        .Text(titre)
                        .FontSize(19)
                        .Bold();

                    column.Item().LineHorizontal(1)
                        .LineColor(QuestPDF.Helpers.Colors.Grey.Lighten1);

                    column.Item().Text(text =>
                    {
                        text.Span("PROPRIÉTAIRE\n").Bold();
                        text.Span(NomProprietaire + "\n");
                        text.Span("Téléphone : " + TelephoneProprietaire + "\n");
                        text.Span("Adresse : " + AdresseProprietaire);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("LOCATAIRE ET LOGEMENT\n").Bold();
                        text.Span("Locataire : " + paiement.NomLocataire + "\n");
                        text.Span("Logement : " + paiement.NomLogement);
                    });

                    column.Item().LineHorizontal(1)
                        .LineColor(QuestPDF.Helpers.Colors.Grey.Lighten1);

                    column.Item().Text(text =>
                    {
                        text.Span("Période du loyer : ").Bold();
                        text.Span(paiement.Periode);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Date du paiement : ").Bold();
                        text.Span(paiement.DatePaiement.ToString("dd/MM/yyyy"));
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Montant payé : ").Bold();
                        text.Span(paiement.MontantTexte);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Mode de paiement : ").Bold();
                        text.Span(paiement.ModePaiement);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Référence : ").Bold();
                        text.Span(paiement.ReferenceTexte);
                    });

                    column.Item()
                        .Background(QuestPDF.Helpers.Colors.Grey.Lighten4)
                        .Padding(12)
                        .Text(text =>
                        {
                            text.Span("Reste à payer sur le loyer : ").Bold();
                            text.Span(paiement.ResteDuTexte);
                        });

                    column.Item().PaddingTop(20).Text(
                        paiement.ResteDu <= 0
                            ? "Le loyer indiqué est entièrement réglé."
                            : "Ce document confirme le paiement indiqué ci-dessus. Un solde reste dû.");

                    column.Item().PaddingTop(35).AlignRight().Text(
                        "Le propriétaire");

                    column.Item().AlignRight()
                        .Text(NomProprietaire)
                        .Bold();
                });

                page.Footer().AlignCenter().Text(
                    "Document généré par Loyercam");
            });
        }).GeneratePdf();
    }
}
