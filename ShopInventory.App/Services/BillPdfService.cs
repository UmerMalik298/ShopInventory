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
        // BC88AC: 80mm wide, usable ~72mm after margins
        // 1mm = 2.8346pt
        private const float PageWidthPt = 80f * 2.8346f;  // 226.8pt
        private const float MarginPt = 8f;                 // 8pt each side
        private const float RowPt = 13f;                   // height per data row
        private const float ItemRowPt = 15f;               // height per bill item

        public byte[] GeneratePdf(Bill bill)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Calculate exact height — no guessing
            float h = 0;

            h += 16;  // shop name
            h += 13;  // arabic name
            h += 11;  // subtitle
            h += 6;   // spacer

            h += RowPt;  // bill no
            h += RowPt;  // date
            if (!string.IsNullOrEmpty(bill.CustomerName)) h += RowPt;
            if (!string.IsNullOrEmpty(bill.CustomerPhone)) h += RowPt;
            h += 8;  // bottom padding of meta block

            h += 4;  // spacer
            h += RowPt + 4;  // items header row

            foreach (var item in bill.Items)
            {
                // Multi-line item names take more height
                var name = string.IsNullOrEmpty(item.VariantName)
                    ? item.ProductName
                    : $"{item.ProductName} ({item.VariantName})";
                var lines = (int)Math.Ceiling(name.Length / 18.0); // ~18 chars per line at font 8
                h += Math.Max(ItemRowPt, lines * 11f);
            }

            h += 6;   // spacer
            h += RowPt;  // subtotal
            if (bill.DiscountAmount > 0) h += RowPt;
            h += RowPt + 6;  // grand total (slightly bigger)

            h += 6;   // spacer
            h += RowPt;  // payment method
            h += RowPt;  // payment status

            if (!string.IsNullOrEmpty(bill.Notes)) h += RowPt + 4;

            h += 8;   // spacer before footer
            h += RowPt;  // thank you
            h += 10;  // footer bottom text
            h += 6;   // final bottom padding

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(PageWidthPt, h));
                    page.MarginLeft(MarginPt);
                    page.MarginRight(MarginPt);
                    page.MarginTop(4);
                    page.MarginBottom(4);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Header ──
                        col.Item().AlignCenter().Text("SHEER RABBANI AUTOS")
                            .FontSize(11).Bold();
                        col.Item().AlignCenter().Text("شیر ربانی آٹوز")
                            .FontSize(9);
                        col.Item().AlignCenter().Text("Receipt / Invoice")
                            .FontSize(7).FontColor(QColors.Grey.Medium);
                        col.Item().Height(4);

                        // ── Meta ──
                        col.Item().BorderBottom(0.5f).BorderColor(QColors.Black)
                            .PaddingBottom(5).Column(meta =>
                            {
                                meta.Item().PaddingVertical(1).Row(r =>
                                {
                                    r.ConstantItem(42).Text("Bill No:").FontSize(7.5f).FontColor(QColors.Grey.Darken2);
                                    r.RelativeItem().AlignRight().Text(bill.BillNo).Bold().FontSize(7.5f);
                                });
                                meta.Item().PaddingVertical(1).Row(r =>
                                {
                                    r.ConstantItem(42).Text("Date:").FontSize(7.5f).FontColor(QColors.Grey.Darken2);
                                    r.RelativeItem().AlignRight()
                                        .Text(bill.BilledAt.ToLocalTime().ToString("dd/MM/yy hh:mm tt"))
                                        .FontSize(7.5f);
                                });
                                if (!string.IsNullOrEmpty(bill.CustomerName))
                                    meta.Item().PaddingVertical(1).Row(r =>
                                    {
                                        r.ConstantItem(42).Text("Customer:").FontSize(7.5f).FontColor(QColors.Grey.Darken2);
                                        r.RelativeItem().AlignRight().Text(bill.CustomerName).FontSize(7.5f);
                                    });
                                if (!string.IsNullOrEmpty(bill.CustomerPhone))
                                    meta.Item().PaddingVertical(1).Row(r =>
                                    {
                                        r.ConstantItem(42).Text("Phone:").FontSize(7.5f).FontColor(QColors.Grey.Darken2);
                                        r.RelativeItem().AlignRight().Text(bill.CustomerPhone).FontSize(7.5f);
                                    });
                            });

                        col.Item().Height(3);

                        // ── Items Header ──
                        col.Item().BorderBottom(0.5f).BorderColor(QColors.Black)
                            .PaddingBottom(3).Row(r =>
                            {
                                r.RelativeItem().Text("Item").Bold().FontSize(7.5f);
                                r.ConstantItem(20).AlignCenter().Text("Qty").Bold().FontSize(7.5f);
                                r.ConstantItem(28).AlignRight().Text("Price").Bold().FontSize(7.5f);
                                r.ConstantItem(30).AlignRight().Text("Total").Bold().FontSize(7.5f);
                            });

                        // ── Items ──
                        foreach (var item in bill.Items)
                        {
                            var name = string.IsNullOrEmpty(item.VariantName)
                                ? item.ProductName
                                : $"{item.ProductName} ({item.VariantName})";

                            col.Item().BorderBottom(0.3f).BorderColor(QColors.Grey.Lighten2)
                                .PaddingVertical(2).Row(r =>
                                {
                                    r.RelativeItem().Text(name).FontSize(7.5f);
                                    r.ConstantItem(20).AlignCenter().Text(item.Quantity.ToString()).FontSize(7.5f);
                                    r.ConstantItem(28).AlignRight().Text($"{item.UnitPrice:N0}").FontSize(7.5f);
                                    r.ConstantItem(30).AlignRight().Text($"{item.TotalPrice:N0}").Bold().FontSize(7.5f);
                                });
                        }

                        col.Item().Height(4);

                        // ── Totals ──
                        col.Item().BorderTop(0.5f).BorderColor(QColors.Black).PaddingTop(3).Column(totals =>
                        {
                            totals.Item().PaddingVertical(1).Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:").FontSize(7.5f).FontColor(QColors.Grey.Darken1);
                                r.ConstantItem(55).AlignRight().Text($"Rs.{bill.SubTotal:N0}").FontSize(7.5f);
                            });
                            if (bill.DiscountAmount > 0)
                                totals.Item().PaddingVertical(1).Row(r =>
                                {
                                    r.RelativeItem().Text("Discount:").FontSize(7.5f).FontColor(QColors.Grey.Darken1);
                                    r.ConstantItem(55).AlignRight().Text($"-Rs.{bill.DiscountAmount:N0}").FontSize(7.5f);
                                });
                            totals.Item().BorderTop(0.5f).BorderColor(QColors.Black)
                                .PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL").Bold().FontSize(10);
                                    r.ConstantItem(60).AlignRight().Text($"Rs.{bill.TotalAmount:N0}").Bold().FontSize(10);
                                });
                        });

                        col.Item().Height(4);

                        // ── Payment ──
                        col.Item().BorderTop(0.3f).BorderColor(QColors.Grey.Lighten1)
                            .PaddingTop(3).Column(pay =>
                            {
                                pay.Item().PaddingVertical(1).Row(r =>
                                {
                                    r.ConstantItem(50).Text("Payment:").FontSize(7.5f).FontColor(QColors.Grey.Darken1);
                                    r.RelativeItem().AlignRight().Text(bill.PaymentMethod.ToString()).FontSize(7.5f);
                                });
                                pay.Item().PaddingVertical(1).Row(r =>
                                {
                                    r.ConstantItem(50).Text("Status:").FontSize(7.5f).FontColor(QColors.Grey.Darken1);
                                    var statusColor = bill.PaymentStatus == PaymentStatus.Paid
                                        ? QColors.Green.Darken2
                                        : bill.PaymentStatus == PaymentStatus.Unpaid
                                            ? QColors.Orange.Darken2
                                            : QColors.Blue.Darken2;
                                    r.RelativeItem().AlignRight()
                                        .Text(bill.PaymentStatus.ToString())
                                        .FontColor(statusColor).Bold().FontSize(7.5f);
                                });
                            });

                        if (!string.IsNullOrEmpty(bill.Notes))
                        {
                            col.Item().Height(3);
                            col.Item().Text($"Note: {bill.Notes}")
                                .Italic().FontSize(7).FontColor(QColors.Grey.Medium);
                        }

                        col.Item().Height(6);

                        // ── Footer ──
                        col.Item().BorderTop(0.3f).BorderColor(QColors.Grey.Lighten2)
                            .PaddingTop(4).AlignCenter()
                            .Text("Thank you for your business!")
                            .FontSize(7.5f).FontColor(QColors.Grey.Medium).Italic();
                        col.Item().AlignCenter()
                            .Text("Please come again")
                            .FontSize(7).FontColor(QColors.Grey.Lighten1);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}