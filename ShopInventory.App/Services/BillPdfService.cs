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
        private const float PrinterWidthMm = 80f;
        private const float MmToPt = 2.8346f;

        public byte[] GeneratePdf(Bill bill)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Precise dynamic height
            float height = 0;
            height += 55;
            height += 4;
            height += 14;
            height += 14;
            if (!string.IsNullOrEmpty(bill.CustomerName)) height += 14;
            if (!string.IsNullOrEmpty(bill.CustomerPhone)) height += 14;
            height += 12;
            height += 16;
            height += bill.Items.Count * 22;
            height += 8;
            height += 18;
            if (bill.DiscountAmount > 0) height += 14;
            height += 22;
            height += 8;
            height += 18;
            height += 18;
            if (!string.IsNullOrEmpty(bill.Notes)) height += 20;
            height += 10;
            height += 30;
            height += 10;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(PrinterWidthMm * MmToPt, height));
                    page.Margin(8);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().AlignCenter().Text("SHEER RABBANI AUTOS").FontSize(12).Bold();
                        col.Item().AlignCenter().Text("شیر ربانی آٹوز").FontSize(9);
                        col.Item().AlignCenter().Text("Receipt / Invoice").FontSize(7).FontColor(QColors.Grey.Medium);
                        col.Item().Height(4);

                        // Meta
                        col.Item().BorderBottom(0.5f).BorderColor(QColors.Black).PaddingBottom(4).Column(meta =>
                        {
                            meta.Item().Row(r =>
                            {
                                r.ConstantItem(45).Text("Bill No:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text(bill.BillNo).Bold().FontSize(8);
                            });
                            meta.Item().Row(r =>
                            {
                                r.ConstantItem(45).Text("Date:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text(bill.BilledAt.ToString("dd/MM/yy hh:mm tt")).FontSize(8);
                            });
                            if (!string.IsNullOrEmpty(bill.CustomerName))
                                meta.Item().Row(r =>
                                {
                                    r.ConstantItem(45).Text("Customer:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                    r.RelativeItem().AlignRight().Text(bill.CustomerName).FontSize(8);
                                });
                            if (!string.IsNullOrEmpty(bill.CustomerPhone))
                                meta.Item().Row(r =>
                                {
                                    r.ConstantItem(45).Text("Phone:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                    r.RelativeItem().AlignRight().Text(bill.CustomerPhone).FontSize(8);
                                });
                        });

                        col.Item().Height(4);

                        // Items Header
                        col.Item().BorderBottom(0.5f).BorderColor(QColors.Black).PaddingBottom(3).Row(r =>
                        {
                            r.RelativeItem().Text("Item").Bold().FontSize(8);
                            r.ConstantItem(22).AlignCenter().Text("Qty").Bold().FontSize(8);
                            r.ConstantItem(32).AlignRight().Text("Price").Bold().FontSize(8);
                            r.ConstantItem(32).AlignRight().Text("Total").Bold().FontSize(8);
                        });

                        // Items
                        foreach (var item in bill.Items)
                        {
                            var name = string.IsNullOrEmpty(item.VariantName)
                                ? item.ProductName
                                : $"{item.ProductName} ({item.VariantName})";

                            col.Item().BorderBottom(0.3f).BorderColor(QColors.Grey.Lighten2).PaddingVertical(3).Row(r =>
                            {
                                r.RelativeItem().Text(name).FontSize(8);
                                r.ConstantItem(22).AlignCenter().Text(item.Quantity.ToString()).FontSize(8);
                                r.ConstantItem(32).AlignRight().Text($"{item.UnitPrice:N0}").FontSize(8);
                                r.ConstantItem(32).AlignRight().Text($"{item.TotalPrice:N0}").Bold().FontSize(8);
                            });
                        }

                        col.Item().Height(4);

                        // Totals
                        col.Item().BorderTop(0.5f).BorderColor(QColors.Black).PaddingTop(4).Column(totals =>
                        {
                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                r.ConstantItem(60).AlignRight().Text($"Rs.{bill.SubTotal:N0}").FontSize(8);
                            });
                            if (bill.DiscountAmount > 0)
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Discount:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                    r.ConstantItem(60).AlignRight().Text($"-Rs.{bill.DiscountAmount:N0}").FontSize(8);
                                });
                            totals.Item().BorderTop(0.5f).BorderColor(QColors.Black).PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL").Bold().FontSize(10);
                                r.ConstantItem(65).AlignRight().Text($"Rs.{bill.TotalAmount:N0}").Bold().FontSize(10);
                            });
                        });

                        col.Item().Height(4);

                        // Payment
                        col.Item().BorderTop(0.3f).BorderColor(QColors.Grey.Lighten1).PaddingTop(4).Column(pay =>
                        {
                            pay.Item().Row(r =>
                            {
                                r.ConstantItem(55).Text("Payment:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text(bill.PaymentMethod.ToString()).FontSize(8);
                            });
                            pay.Item().Row(r =>
                            {
                                r.ConstantItem(55).Text("Status:").FontSize(8).FontColor(QColors.Grey.Darken1);
                                var statusColor = bill.PaymentStatus == PaymentStatus.Paid
                                    ? QColors.Green.Darken1
                                    : bill.PaymentStatus == PaymentStatus.Unpaid
                                        ? QColors.Orange.Darken1
                                        : QColors.Blue.Darken1;
                                r.RelativeItem().AlignRight().Text(bill.PaymentStatus.ToString())
                                    .FontColor(statusColor).Bold().FontSize(8);
                            });
                        });

                        if (!string.IsNullOrEmpty(bill.Notes))
                        {
                            col.Item().Height(4);
                            col.Item().Text($"Note: {bill.Notes}").Italic().FontSize(7).FontColor(QColors.Grey.Medium);
                        }

                        col.Item().Height(6);

                        // Footer
                        col.Item().BorderTop(0.3f).BorderColor(QColors.Grey.Lighten1).PaddingTop(5)
                            .AlignCenter().Text("Thank you for your business!")
                            .FontSize(8).FontColor(QColors.Grey.Medium).Italic();
                        col.Item().AlignCenter().Text("Please come again")
                            .FontSize(7).FontColor(QColors.Grey.Lighten1);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}