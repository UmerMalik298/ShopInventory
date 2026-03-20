using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;
using QColors = QuestPDF.Helpers.Colors;

namespace ShopInventory.App.Services
{
    public class BillPdfService
    {
        private const float PageWidthPt = 80f * 2.8346f;
        private const float MarginPt = 5f;
        private const float RowPt = 16f;
        private const float ItemRowPt = 17f;

        public byte[] GeneratePdf(Bill bill)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // ── Exact height calculation ──
            float h = 0;

            h += 14;  // shop name     (14pt font = ~16pt height)
            h += 12;  // arabic name   (11pt font = ~13pt height)
            h += 10;  // subtitle line (8.5pt font = ~11pt height)
            h += 4;   // spacer
            h += 2;   // divider line
            h += 4;  // spacer after divider

            h += RowPt;  // bill no
            h += RowPt;  // date
            if (!string.IsNullOrEmpty(bill.CustomerName)) h += RowPt;
            if (!string.IsNullOrEmpty(bill.CustomerPhone)) h += RowPt;
            h += 10;  // meta block bottom padding

            h += 2;   // divider
            h += 6;   // spacer

            h += RowPt + 6;  // items header row

            h += 2;   // divider under header

            foreach (var item in bill.Items)
            {
                var name = string.IsNullOrEmpty(item.VariantName)
                    ? item.ProductName
                    : $"{item.ProductName} ({item.VariantName})";
                var lines = (int)Math.Ceiling(name.Length / 15.0);
                h += Math.Max(ItemRowPt, lines * 14f);
            }

            h += 6;   // spacer
            h += 2;   // divider
            h += 6;   // spacer

            h += RowPt;  // subtotal
            if (bill.DiscountAmount > 0) h += RowPt;

            h += 4;   // spacer
            h += 2;   // divider
            h += 4;   // spacer

            h += RowPt + 10; // TOTAL row (bigger font = taller)

            h += 2;   // divider
            h += 6;   // spacer

            h += RowPt;  // payment method
            h += RowPt;  // payment status

            if (!string.IsNullOrEmpty(bill.Notes)) h += RowPt + 6;

            h += 10;  // spacer before footer
            h += 2;   // divider
            h += 6;   // spacer
            h += RowPt;  // thank you
            h += 14;     // please come again
            h += 10;     // bottom breathing room

            // ── NO big safety buffer needed now that printer is on 3276mm mode ──
            // ── But keep a tiny one just in case ──
            h += 8f;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(PageWidthPt, h));
                    page.MarginLeft(MarginPt);
                    page.MarginRight(MarginPt);
                    page.MarginTop(-2);
                    page.MarginBottom(2);

                    // Everything bold and black by default
                    page.DefaultTextStyle(x => x
                        .FontSize(9)
                        .FontFamily("Arial")
                        .Bold()
                        .FontColor(QColors.Black));

                    page.Content().Column(col =>
                    {
                        // ── Header ──
                        col.Item().AlignCenter().Text("SHEER RABBANI AUTOS")
                            .FontSize(14).Bold();

                        col.Item().AlignCenter().Text("شیر ربانی آٹوز")
       .FontSize(11).Bold();
                        col.Item().AlignCenter().Text("Receipt / Invoice")
                            .FontSize(8.5f).NormalWeight();

                        col.Item().Height(3);
                        col.Item().BorderBottom(1f).BorderColor(QColors.Black).Height(1);
                        col.Item().Height(3);

                        // ── Meta ──
                        col.Item().Column(meta =>
                        {
                            meta.Item().PaddingVertical(2).Row(r =>
                            {
                                r.ConstantItem(48).Text("Bill No:").FontSize(9f);
                                r.RelativeItem().AlignRight().Text(bill.BillNo).FontSize(9f);
                            });
                            meta.Item().PaddingVertical(2).Row(r =>
                            {
                                r.ConstantItem(48).Text("Date:").FontSize(9f);
                                r.RelativeItem().AlignRight()
                                    .Text(bill.BilledAt.ToLocalTime()
                                        .ToString("dd/MM/yy hh:mm tt"))
                                    .FontSize(9f);
                            });
                            if (!string.IsNullOrEmpty(bill.CustomerName))
                                meta.Item().PaddingVertical(2).Row(r =>
                                {
                                    r.ConstantItem(48).Text("Customer:").FontSize(9f);
                                    r.RelativeItem().AlignRight()
                                        .Text(bill.CustomerName).FontSize(9f);
                                });
                            if (!string.IsNullOrEmpty(bill.CustomerPhone))
                                meta.Item().PaddingVertical(2).Row(r =>
                                {
                                    r.ConstantItem(48).Text("Phone:").FontSize(9f);
                                    r.RelativeItem().AlignRight()
                                        .Text(bill.CustomerPhone).FontSize(9f);
                                });
                        });

                        col.Item().Height(5);
                        col.Item().BorderBottom(1f).BorderColor(QColors.Black).Height(1);
                        col.Item().Height(5);

                        // ── Items Header ──
                        col.Item().PaddingVertical(3).Row(r =>
                        {
                            r.RelativeItem().Text("ITEM").FontSize(9f);
                            r.ConstantItem(24).AlignCenter().Text("QTY").FontSize(9f);
                            r.ConstantItem(32).AlignRight().Text("PRICE").FontSize(9f);
                            r.ConstantItem(34).AlignRight().Text("TOTAL").FontSize(9f);
                        });

                        col.Item().BorderBottom(1f).BorderColor(QColors.Black).Height(1);

                        // ── Items ──
                        foreach (var item in bill.Items)
                        {
                            var name = string.IsNullOrEmpty(item.VariantName)
                                ? item.ProductName
                                : $"{item.ProductName} ({item.VariantName})";

                            col.Item().BorderBottom(0.5f)
                                .BorderColor(QColors.Grey.Darken2)
                                .PaddingVertical(3).Row(r =>
                                {
                                    r.RelativeItem().Text(name).FontSize(9f);
                                    r.ConstantItem(24).AlignCenter()
                                        .Text(item.Quantity.ToString()).FontSize(9f);
                                    r.ConstantItem(32).AlignRight()
                                        .Text($"{item.UnitPrice:N0}").FontSize(9f);
                                    r.ConstantItem(34).AlignRight()
                                        .Text($"{item.TotalPrice:N0}").FontSize(9f);
                                });
                        }

                        col.Item().Height(5);
                        col.Item().BorderBottom(1f).BorderColor(QColors.Black).Height(1);
                        col.Item().Height(5);

                        // ── Subtotal / Discount ──
                        col.Item().Column(totals =>
                        {
                            totals.Item().PaddingVertical(2).Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:").FontSize(9f);
                                r.ConstantItem(65).AlignRight()
                                    .Text($"Rs. {bill.SubTotal:N0}").FontSize(9f);
                            });
                            if (bill.DiscountAmount > 0)
                                totals.Item().PaddingVertical(2).Row(r =>
                                {
                                    r.RelativeItem().Text("Discount:").FontSize(9f);
                                    r.ConstantItem(65).AlignRight()
                                        .Text($"-Rs. {bill.DiscountAmount:N0}").FontSize(9f);
                                });
                        });

                        col.Item().Height(4);
                        col.Item().BorderBottom(1f).BorderColor(QColors.Black).Height(1);
                        col.Item().Height(4);

                        // ── Grand Total ── largest text on the whole receipt
                        col.Item().PaddingVertical(4).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL").FontSize(15).Bold();
                            r.ConstantItem(75).AlignRight()
                                .Text($"Rs. {bill.TotalAmount:N0}").FontSize(15).Bold();
                        });

                        col.Item().BorderBottom(1f).BorderColor(QColors.Black).Height(1);
                        col.Item().Height(5);

                        // ── Payment ──
                        col.Item().Column(pay =>
                        {
                            pay.Item().PaddingVertical(2).Row(r =>
                            {
                                r.ConstantItem(52).Text("Payment:").FontSize(9f);
                                r.RelativeItem().AlignRight()
                                    .Text(bill.PaymentMethod.ToString()).FontSize(9f);
                            });
                            pay.Item().PaddingVertical(2).Row(r =>
                            {
                                r.ConstantItem(52).Text("Status:").FontSize(9f);
                                r.RelativeItem().AlignRight()
                                    .Text(bill.PaymentStatus.ToString()).FontSize(9f);
                            });
                        });

                        if (!string.IsNullOrEmpty(bill.Notes))
                        {
                            col.Item().Height(4);
                            col.Item().BorderTop(0.5f).BorderColor(QColors.Black)
                                .PaddingTop(3)
                                .Text($"Note: {bill.Notes}")
                                .FontSize(8.5f).NormalWeight().Italic();
                        }

                        col.Item().Height(10);
                        col.Item().BorderBottom(1f).BorderColor(QColors.Black).Height(1);
                        col.Item().Height(5);

                        // ── Footer ──
                        col.Item().AlignCenter()
                            .Text("** Thank You For Your Business! **")
                            .FontSize(9f).Bold();

                        col.Item().PaddingTop(3).AlignCenter()
                            .Text("Please Come Again")
                            .FontSize(8.5f).NormalWeight();

                        col.Item().Height(8);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}