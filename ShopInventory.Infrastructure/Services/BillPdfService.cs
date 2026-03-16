using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;

namespace ShopInventory.App.Services
{
    public class BillPdfService
    {
        // Change this to 58 if you have a 58mm printer
        private const float PrinterWidthMm = 80f;

        public byte[] GeneratePdf(Bill bill)
        {
            QuestPDF.Settings.License = LicenseType.Community;


            float dynamicHeight = 280 + (bill.Items.Count * 20);
            if (!string.IsNullOrEmpty(bill.Notes)) dynamicHeight += 20;
            if (!string.IsNullOrEmpty(bill.CustomerName)) dynamicHeight += 15;
            if (!string.IsNullOrEmpty(bill.CustomerPhone)) dynamicHeight += 15;
            if (bill.DiscountAmount > 0) dynamicHeight += 15;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(PrinterWidthMm * 2.8346f, dynamicHeight));
                    page.Margin(6);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Header ──
                        col.Item().AlignCenter().Text("SHEER RABBANI AUTOS")
                            .FontSize(13).Bold();
                        col.Item().AlignCenter().Text("شیر ربانی آٹوز")
                            .FontSize(10);
                        col.Item().AlignCenter().Text("Receipt / Invoice")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().Height(4);

                        // ── Divider ──
                        col.Item().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(4).Column(meta =>
                        {
                            meta.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Bill No:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text(bill.BillNo).Bold().FontSize(8);
                            });
                            meta.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Date:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text(bill.BilledAt.ToString("dd/MM/yyyy hh:mm tt")).FontSize(8);
                            });
                            if (!string.IsNullOrEmpty(bill.CustomerName))
                            {
                                meta.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Customer:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    r.RelativeItem().AlignRight().Text(bill.CustomerName).FontSize(8);
                                });
                            }
                            if (!string.IsNullOrEmpty(bill.CustomerPhone))
                            {
                                meta.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Phone:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    r.RelativeItem().AlignRight().Text(bill.CustomerPhone).FontSize(8);
                                });
                            }
                        });

                        col.Item().Height(4);

                        // ── Items Header ──
                        col.Item().BorderBottom(0.5f).BorderColor(Colors.Black)
                            .PaddingBottom(3).Row(r =>
                            {
                                r.RelativeItem(5).Text("Item").Bold().FontSize(8);
                                r.ConstantItem(20).AlignCenter().Text("Qty").Bold().FontSize(8);
                                r.ConstantItem(35).AlignRight().Text("Price").Bold().FontSize(8);
                                r.ConstantItem(38).AlignRight().Text("Total").Bold().FontSize(8);
                            });

                        // ── Items ──
                        foreach (var item in bill.Items)
                        {
                            var name = string.IsNullOrEmpty(item.VariantName)
                                ? item.ProductName
                                : $"{item.ProductName}\n({item.VariantName})";

                            col.Item().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(3).Row(r =>
                                {
                                    r.RelativeItem(5).Text(name).FontSize(8);
                                    r.ConstantItem(20).AlignCenter().Text(item.Quantity.ToString()).FontSize(8);
                                    r.ConstantItem(35).AlignRight().Text($"{item.UnitPrice:N0}").FontSize(8);
                                    r.ConstantItem(38).AlignRight().Text($"{item.TotalPrice:N0}").Bold().FontSize(8);
                                });
                        }

                        col.Item().Height(4);

                        // ── Totals ──
                        col.Item().BorderTop(0.5f).BorderColor(Colors.Black).PaddingTop(4).Column(totals =>
                        {
                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text($"Rs. {bill.SubTotal:N0}").FontSize(8);
                            });

                            if (bill.DiscountAmount > 0)
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Discount:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    r.RelativeItem().AlignRight().Text($"- Rs. {bill.DiscountAmount:N0}").FontSize(8);
                                });
                            }

                            // Grand total — bigger, bold
                            totals.Item().BorderTop(0.5f).BorderColor(Colors.Black)
                                .PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL").Bold().FontSize(11);
                                    r.RelativeItem().AlignRight().Text($"Rs. {bill.TotalAmount:N0}").Bold().FontSize(11);
                                });
                        });

                        col.Item().Height(4);

                        // ── Payment ──
                        col.Item().BorderTop(0.3f).BorderColor(Colors.Grey.Lighten1).PaddingTop(4).Column(pay =>
                        {
                            pay.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Payment:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem().AlignRight().Text(bill.PaymentMethod.ToString()).FontSize(8);
                            });
                            pay.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Status:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                var statusColor = bill.PaymentStatus == PaymentStatus.Paid
                                    ? Colors.Green.Darken1
                                    : bill.PaymentStatus == PaymentStatus.Unpaid
                                        ? Colors.Orange.Darken1
                                        : Colors.Blue.Darken1;
                                r.RelativeItem().AlignRight().Text(bill.PaymentStatus.ToString())
                                    .FontColor(statusColor).Bold().FontSize(8);
                            });
                        });

                        if (!string.IsNullOrEmpty(bill.Notes))
                        {
                            col.Item().Height(4);
                            col.Item().Text($"Note: {bill.Notes}").Italic().FontSize(7).FontColor(Colors.Grey.Medium);
                        }

                        col.Item().Height(8);

                        // ── Footer ──
                        col.Item().BorderTop(0.3f).BorderColor(Colors.Grey.Lighten1).PaddingTop(6)
                            .AlignCenter().Text("Thank you for your business!")
                            .FontSize(8).FontColor(Colors.Grey.Medium).Italic();

                        col.Item().AlignCenter().Text("Please come again")
                            .FontSize(7).FontColor(Colors.Grey.Lighten1);

                        col.Item().Height(6);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}