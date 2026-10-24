
#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using AndroidPaint = Android.Graphics.Paint;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidPaintFlags = Android.Graphics.PaintFlags;
#else
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
#endif

namespace Loyercam.Pages;

public static class QuittancePdfService
{
    private const string NomProprietaire = "DADA NELSON";
    private const string TelephoneProprietaire = "657526945";
    private const string AdresseProprietaire = "Douala, Cameroun";

    public static byte[] GenererPdf(QuittanceItem paiement)
    {
        ArgumentNullException.ThrowIfNull(paiement);

#if ANDROID
        return GenererPdfAndroid(paiement);
#else
        return GenererPdfQuestPdf(paiement);
#endif
    }

#if ANDROID

    private static byte[] GenererPdfAndroid(QuittanceItem paiement)
    {
        using var document = new PdfDocument();

        const int largeur = 595;
        const int hauteur = 842;
        const float marge = 40;

        var pageInfo = new PdfDocument.PageInfo.Builder(
            largeur, hauteur, 1).Create();

        var page = document.StartPage(pageInfo);
        AndroidCanvas? canvas = page.Canvas;

        if (canvas is null)
        {
            document.FinishPage(page);

            throw new InvalidOperationException(
                "Impossible de créer le contenu du PDF.");
        }

        try
        {
            using var titrePaint = new AndroidPaint(
                AndroidPaintFlags.AntiAlias)
            {
                Color = Android.Graphics.Color.Rgb(35, 75, 140),
                TextSize = 25
            };

            titrePaint.SetTypeface(
                Typeface.Create("sans-serif", TypefaceStyle.Bold));

            using var sousTitrePaint = new AndroidPaint(
                AndroidPaintFlags.AntiAlias)
            {
                Color = Android.Graphics.Color.DarkGray,
                TextSize = 12
            };

            using var textePaint = new AndroidPaint(
                AndroidPaintFlags.AntiAlias)
            {
                Color = Android.Graphics.Color.Black,
                TextSize = 12
            };

            using var grasPaint = new AndroidPaint(
                AndroidPaintFlags.AntiAlias)
            {
                Color = Android.Graphics.Color.Black,
                TextSize = 12
            };

            grasPaint.SetTypeface(
                Typeface.Create("sans-serif", TypefaceStyle.Bold));

            float y = 55;

            canvas.DrawText("LOYERCAM", marge, y, titrePaint);
            y += 25;

            canvas.DrawText("Gestion locative", marge, y, sousTitrePaint);
            y += 35;

            string titre = paiement.ResteDu <= 0
                ? "QUITTANCE DE LOYER"
                : "REÇU DE PAIEMENT DE LOYER";

            DrawWrappedText(
                canvas, titre, grasPaint,
                marge, ref y, largeur - 2 * marge, 22, 12);

            DrawLine(canvas, marge, y, largeur - marge);
            y += 20;

            DrawSection(
                canvas, "PROPRIÉTAIRE", grasPaint, marge, ref y);

            DrawField(
                canvas, "Nom", NomProprietaire,
                textePaint, grasPaint, marge, ref y);

            DrawField(
                canvas, "Téléphone", TelephoneProprietaire,
                textePaint, grasPaint, marge, ref y);

            DrawField(
                canvas, "Adresse", AdresseProprietaire,
                textePaint, grasPaint, marge, ref y);

            y += 8;
            DrawLine(canvas, marge, y, largeur - marge);
            y += 20;

            DrawSection(
                canvas, "LOCATAIRE ET LOGEMENT",
                grasPaint, marge, ref y);

            DrawField(
                canvas, "Locataire", paiement.NomLocataire,
                textePaint, grasPaint, marge, ref y);

            DrawField(
                canvas, "Logement", paiement.NomLogement,
                textePaint, grasPaint, marge, ref y);

            y += 8;
            DrawLine(canvas, marge, y, largeur - marge);
            y += 20;

            DrawField(
                canvas, "Période du loyer", paiement.Periode,
                textePaint, grasPaint, marge, ref y);

            DrawField(
                canvas, "Date du paiement",
                paiement.DatePaiement.ToString("dd/MM/yyyy"),
                textePaint, grasPaint, marge, ref y);

            DrawField(
                canvas, "Montant payé", paiement.MontantTexte,
                textePaint, grasPaint, marge, ref y);

            DrawField(
                canvas, "Mode de paiement", paiement.ModePaiement,
                textePaint, grasPaint, marge, ref y);

            DrawField(
                canvas, "Référence", paiement.ReferenceTexte,
                textePaint, grasPaint, marge, ref y);

            y += 8;
            DrawLine(canvas, marge, y, largeur - marge);
            y += 20;

            DrawField(
                canvas, "Reste à payer sur le loyer",
                paiement.ResteDuTexte,
                textePaint, grasPaint, marge, ref y);

            y += 15;

            string message = paiement.ResteDu <= 0
                ? "Le loyer indiqué est entièrement réglé."
                : "Ce document confirme le paiement indiqué ci-dessus. Un solde reste dû.";

            DrawWrappedText(
                canvas, message, textePaint,
                marge, ref y, largeur - 2 * marge, 18, 10);

            y += 35;

            canvas.DrawText(
                "Le propriétaire", largeur - 155, y, textePaint);
            y += 20;

            canvas.DrawText(
                NomProprietaire, largeur - 155, y, grasPaint);

            using var piedPaint = new AndroidPaint(
                AndroidPaintFlags.AntiAlias)
            {
                Color = Android.Graphics.Color.DarkGray,
                TextSize = 9,
                TextAlign = AndroidPaint.Align.Center
            };

            canvas.DrawText(
                "Document généré par LoyerCam",
                largeur / 2f,
                hauteur - 25,
                piedPaint);
        }
        finally
        {
            document.FinishPage(page);
        }

        using var flux = new MemoryStream();
        document.WriteTo(flux);

        return flux.ToArray();
    }

    private static void DrawSection(
        AndroidCanvas canvas,
        string titre,
        AndroidPaint paint,
        float x,
        ref float y)
    {
        canvas.DrawText(titre, x, y, paint);
        y += 22;
    }

    private static void DrawField(
        AndroidCanvas canvas,
        string libelle,
        string? valeur,
        AndroidPaint textePaint,
        AndroidPaint grasPaint,
        float x,
        ref float y)
    {
        canvas.DrawText(libelle + " :", x, y, grasPaint);
        y += 16;

        string valeurAffichee = string.IsNullOrWhiteSpace(valeur)
            ? "Non renseigné"
            : valeur;

        DrawWrappedText(
            canvas, valeurAffichee, textePaint,
            x + 8, ref y, 480, 17, 5);
    }

    private static void DrawLine(
        AndroidCanvas canvas,
        float x1,
        float y,
        float x2)
    {
        using var paint = new AndroidPaint(
            AndroidPaintFlags.AntiAlias)
        {
            Color = Android.Graphics.Color.LightGray,
            StrokeWidth = 1
        };

        canvas.DrawLine(x1, y, x2, y, paint);
    }

    private static void DrawWrappedText(
        AndroidCanvas canvas,
        string? texte,
        AndroidPaint paint,
        float x,
        ref float y,
        float largeurMax,
        float hauteurLigne,
        float espaceApres = 0)
    {
        texte = string.IsNullOrWhiteSpace(texte)
            ? "Non renseigné"
            : texte;

        string ligne = string.Empty;

        foreach (string mot in texte.Split(
            ' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string essai = string.IsNullOrEmpty(ligne)
                ? mot
                : ligne + " " + mot;

            if (paint.MeasureText(essai) > largeurMax &&
                !string.IsNullOrEmpty(ligne))
            {
                canvas.DrawText(ligne, x, y, paint);
                y += hauteurLigne;
                ligne = mot;
            }
            else
            {
                ligne = essai;
            }
        }

        if (!string.IsNullOrEmpty(ligne))
        {
            canvas.DrawText(ligne, x, y, paint);
            y += hauteurLigne;
        }

        y += espaceApres;
    }

#else

    private static byte[] GenererPdfQuestPdf(QuittanceItem paiement)
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
                    "Document généré par LoyerCam");
            });
        }).GeneratePdf();
    }

#endif
}
