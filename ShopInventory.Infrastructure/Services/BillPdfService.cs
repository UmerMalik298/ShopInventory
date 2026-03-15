using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;

namespace ShopInventory.App.Services
{
    public class BillPdfService
    {
        public byte[] GeneratePdf(Bill bill)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().AlignCenter().Text("YOUR SHOP NAME")
                            .FontSize(20).Bold();
                        col.Item().AlignCenter().Text("Receipt / Invoice")
                            .FontSize(11).FontColor(Colors.Grey.Medium);
                        col.Item().Height(10);

                        // Bill meta
                        col.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingBottom(8).Column(meta =>
                            {
                                meta.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Bill No.").FontColor(Colors.Grey.Medium);
                                    r.RelativeItem().AlignRight().Text(bill.BillNo).Bold().FontFamily("Courier New");
                                });
                                meta.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Date").FontColor(Colors.Grey.Medium);
                                    r.RelativeItem().AlignRight().Text(bill.BilledAt.ToString("dd MMM yyyy, hh:mm tt"));
                                });
                                if (!string.IsNullOrEmpty(bill.CustomerName))
                                {
                                    meta.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Customer").FontColor(Colors.Grey.Medium);
                                        r.RelativeItem().AlignRight().Text(bill.CustomerName);
                                    });
                                }
                                if (!string.IsNullOrEmpty(bill.CustomerPhone))
                                {
                                    meta.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Phone").FontColor(Colors.Grey.Medium);
                                        r.RelativeItem().AlignRight().Text(bill.CustomerPhone);
                                    });
                                }
                            });

                        col.Item().Height(8);

                        // Items table header
                        col.Item().Background(Colors.Grey.Lighten3).Padding(6).Row(r =>
                        {
                            r.RelativeItem(4).Text("Item").Bold();
                            r.RelativeItem(1).AlignCenter().Text("Qty").Bold();
                            r.RelativeItem(2).AlignRight().Text("Price").Bold();
                            r.RelativeItem(2).AlignRight().Text("Total").Bold();
                        });

                        // Items
                        foreach (var item in bill.Items)
                        {
                            var name = string.IsNullOrEmpty(item.VariantName)
                                ? item.ProductName
                                : $"{item.ProductName} ({item.VariantName})";

                            col.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3)
                                .Padding(6).Row(r =>
                                {
                                    r.RelativeItem(4).Text(name);
                                    r.RelativeItem(1).AlignCenter().Text(item.Quantity.ToString());
                                    r.RelativeItem(2).AlignRight().Text($"Rs. {item.UnitPrice:N0}");
                                    r.RelativeItem(2).AlignRight().Text($"Rs. {item.TotalPrice:N0}").Bold();
                                });
                        }

                        col.Item().Height(8);

                        // Totals
                        col.Item().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingTop(8).Column(totals =>
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal").FontColor(Colors.Grey.Medium);
                                    r.RelativeItem().AlignRight().Text($"Rs. {bill.SubTotal:N0}");
                                });
                                if (bill.DiscountAmount > 0)
                                {
                                    totals.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Discount").FontColor(Colors.Green.Medium);
                                        r.RelativeItem().AlignRight().Text($"- Rs. {bill.DiscountAmount:N0}").FontColor(Colors.Green.Medium);
                                    });
                                }
                                totals.Item().Background(Colors.Grey.Lighten3).Padding(6).Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL").Bold().FontSize(12);
                                    r.RelativeItem().AlignRight().Text($"Rs. {bill.TotalAmount:N0}").Bold().FontSize(12);
                                });
                            });

                        col.Item().Height(10);

                        // Payment info
                        col.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(pay =>
                        {
                            pay.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Payment Method").FontColor(Colors.Grey.Medium);
                                r.RelativeItem().AlignRight().Text(bill.PaymentMethod.ToString());
                            });
                            pay.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Payment Status").FontColor(Colors.Grey.Medium);
                                var statusColor = bill.PaymentStatus == PaymentStatus.Paid
                                    ? Colors.Green.Medium
                                    : bill.PaymentStatus == PaymentStatus.Unpaid
                                        ? Colors.Orange.Medium
                                        : Colors.Blue.Medium;
                                r.RelativeItem().AlignRight().Text(bill.PaymentStatus.ToString()).FontColor(statusColor).Bold();
                            });
                        });

                        if (!string.IsNullOrEmpty(bill.Notes))
                        {
                            col.Item().Height(6);
                            col.Item().Text($"Notes: {bill.Notes}").Italic().FontColor(Colors.Grey.Medium);
                        }

                        col.Item().Height(16);

                        // Footer
                        col.Item().AlignCenter().Text("Thank you for your purchase!")
                            .FontColor(Colors.Grey.Medium).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}