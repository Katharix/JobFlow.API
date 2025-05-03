// JobFlow.Infrastructure/Pdf/QuestPdfGenerator.cs
using System.Threading.Tasks;
using JobFlow.Business.DI;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobFlow.Infrastructure.Pdf
{
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
                    page.Header().Text($"Invoice #{inv.Id:D}")
                                   .FontSize(20)
                                   .SemiBold();
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Date: {inv.InvoiceDate:MMMM dd, yyyy}");
                        col.Item().Text($"Due:  {inv.DueDate:MMMM dd, yyyy}")
                                   .Style(TextStyle.Default.FontColor(Colors.Grey.Darken1));
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(def =>
                            {
                                def.RelativeColumn();
                                def.ConstantColumn(60);
                                def.ConstantColumn(80);
                                def.ConstantColumn(80);
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Text("Description");
                                header.Cell().AlignRight().Text("Qty");
                                header.Cell().AlignRight().Text("Unit Price");
                                header.Cell().AlignRight().Text("Total");
                            });

                            // Line items
                            foreach (var li in inv.LineItems)
                            {
                                table.Cell().Text(li.Description);
                                table.Cell().AlignRight().Text(li.Quantity.ToString());
                                table.Cell().AlignRight().Text(li.UnitPrice.ToString("C"));
                                table.Cell().AlignRight().Text(li.LineTotal.ToString("C"));
                            }

                            // Footer totals
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(3)
                                      .AlignRight()
                                      .Text("Grand Total:")
                                      .Bold();
                                footer.Cell()
                                      .AlignRight()
                                      .Text(inv.TotalAmount.ToString("C"))
                                      .Bold();
                            });
                        });
                    });
                    page.Footer().AlignCenter().Text($"Thank you for choosing Job Flow!");
                });
            });

            byte[] pdf = document.GeneratePdf();
            return Task.FromResult(pdf);
        }
    }
}
