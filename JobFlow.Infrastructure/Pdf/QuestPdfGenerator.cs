// JobFlow.Infrastructure/Pdf/QuestPdfGenerator.cs

using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobFlow.Infrastructure.Pdf;

[ScopedService]
public class QuestPdfGenerator : IPdfGenerator
{
    public Task<byte[]> GenerateInvoicePdfAsync(Invoice inv)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(inv.OrganizationClient.Organization.OrganizationName).FontSize(24).Bold();
                        //col.Item().Text("NobleUI Themes").Bold();
                        col.Item().Text($"{inv.OrganizationClient.Organization.Address1}," +
                                        $"\n{inv.OrganizationClient.Organization.City}, {inv.OrganizationClient.Organization.State}, {inv.OrganizationClient.Organization.ZipCode}");
                        col.Item().PaddingTop(20).Text("Invoice to:").Bold().FontColor(Colors.Grey.Darken1);
                        col.Item().Text(
                            $"{inv.OrganizationClient.ClientFullName()}\n{inv.OrganizationClient.Address1}" +
                            $"\n{inv.OrganizationClient.City}, {inv.OrganizationClient.State}, {inv.OrganizationClient.ZipCode}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text("INVOICE").FontSize(18).Bold();
                        col.Item().PaddingBottom(20).AlignRight().Text($"# {inv.InvoiceNumber}").FontSize(12);
                        col.Item().AlignRight().Text("Balance Due");
                        col.Item().AlignRight().Text(inv.BalanceDue.ToString("C")).FontSize(16).Bold();
                        col.Item().PaddingTop(20).AlignRight().Text($"Invoice Date: {inv.InvoiceDate:dd MMM yyyy}")
                            .FontSize(10);
                        col.Item().AlignRight().Text($"Due Date: {inv.DueDate:dd MMM yyyy}").FontSize(10);
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("#").Bold();
                            header.Cell().Text("Description").Bold();
                            header.Cell().AlignRight().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Unit Price").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                        });

                        var index = 1;
                        foreach (var item in inv.LineItems)
                        {
                            table.Cell().Text(index++.ToString());
                            table.Cell().Text(item.Description);
                            table.Cell().AlignRight().Text(item.Quantity.ToString());
                            table.Cell().AlignRight().Text(item.UnitPrice.ToString("C"));
                            table.Cell().AlignRight().Text(item.LineTotal.ToString("C"));
                        }
                    });

                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem();

                        row.ConstantItem(200).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            table.Cell().Text("Sub Total");
                            table.Cell().AlignRight().Text(inv.LineItems.Sum(x => x.LineTotal).ToString("C"));

                            table.Cell().Text("TAX (12%)");
                            table.Cell().AlignRight().Text((inv.LineItems.Sum(x => x.LineTotal) * 0.12m).ToString("C"));

                            table.Cell().Text("Total").Bold();
                            var totalWithTax = inv.LineItems.Sum(x => x.LineTotal) * 1.12m;
                            table.Cell().AlignRight().Text(totalWithTax.ToString("C")).Bold();

                            table.Cell().Text("Payment Made");
                            table.Cell().AlignRight().Text($"(-){inv.AmountPaid.ToString("C")}")
                                .FontColor(Colors.Red.Darken1);

                            table.Cell().Text("Balance Due").Bold();
                            table.Cell().AlignRight().Text(inv.BalanceDue.ToString("C")).Bold();
                        });
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"Thank you for choosing {inv.OrganizationClient.Organization.OrganizationName}!")
                    .FontSize(10);
            });
        });

        var pdf = document.GeneratePdf();
        return Task.FromResult(pdf);
    }

    public Task<byte[]> GenerateEstimatePdfAsync(Estimate estimate)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Content().Column(col =>
                {
                    col.Item().Text($"Estimate {estimate.EstimateNumber}").FontSize(18).Bold();
                    col.Item().Text($"Created: {estimate.CreatedAt:MMM dd, yyyy}");
                    col.Item().Text($"Status: {estimate.Status}");

                    if (!string.IsNullOrWhiteSpace(estimate.Title))
                        col.Item().PaddingTop(10).Text(estimate.Title).Bold();

                    if (!string.IsNullOrWhiteSpace(estimate.Description))
                        col.Item().Text(estimate.Description);

                    col.Item().PaddingTop(15);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(6);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Item").Bold();
                            header.Cell().AlignRight().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Price").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                        });

                        foreach (var li in estimate.LineItems)
                        {
                            table.Cell().Text(li.Name);
                            table.Cell().AlignRight().Text(li.Quantity.ToString("0.##"));
                            table.Cell().AlignRight().Text(li.UnitPrice.ToString("C"));
                            table.Cell().AlignRight().Text(li.Total.ToString("C"));
                        }
                    });

                    col.Item().PaddingTop(15);
                    col.Item().AlignRight().Text($"Subtotal: {estimate.Subtotal:C}");
                    col.Item().AlignRight().Text($"Tax: {estimate.TaxTotal:C}");
                    col.Item().AlignRight().Text($"Total: {estimate.Total:C}").FontSize(14).Bold();

                    if (!string.IsNullOrWhiteSpace(estimate.Notes))
                    {
                        col.Item().PaddingTop(20);
                        col.Item().Text("Notes").Bold();
                        col.Item().Text(estimate.Notes);
                    }
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}