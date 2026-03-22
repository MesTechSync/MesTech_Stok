using System.Diagnostics;
using ClosedXML.Excel;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// Bulk product import tests using ClosedXML in-memory workbooks.
/// Validates Excel parsing, duplicate SKU handling, validation rules, and performance.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Platform")]
public class BulkProductImportTests
{
    // ═══════════════════════════════════════════════════════════
    // Helper: Create an in-memory Excel workbook with product data
    // ═══════════════════════════════════════════════════════════

    private static MemoryStream CreateExcelStream(
        string[] headers,
        List<object[]> rows)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        // Write headers
        for (int col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
        }

        // Write data rows
        for (int row = 0; row < rows.Count; row++)
        {
            for (int col = 0; col < rows[row].Length; col++)
            {
                var value = rows[row][col];
                var cell = worksheet.Cell(row + 2, col + 1);

                if (value is decimal d)
                    cell.Value = (double)d;
                else if (value is int i)
                    cell.Value = i;
                else if (value is double dbl)
                    cell.Value = dbl;
                else
                    cell.Value = value?.ToString() ?? string.Empty;
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static (List<Product> imported, List<string> errors) ParseExcelImport(
        Stream excelStream,
        Dictionary<string, Product>? existingProducts = null,
        bool updateExisting = false)
    {
        var imported = new List<Product>();
        var errors = new List<string>();
        existingProducts ??= new Dictionary<string, Product>();

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();

        // Read headers from row 1
        var headerRow = worksheet.Row(1);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= worksheet.LastColumnUsed()?.ColumnNumber(); col++)
        {
            var headerValue = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(headerValue))
                headers[headerValue] = col;
        }

        // Validate required columns
        if (!headers.ContainsKey("SKU"))
        {
            errors.Add("Missing required column: SKU");
            return (imported, errors);
        }

        // Parse data rows
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var sku = worksheet.Cell(row, headers["SKU"]).GetString().Trim();
                if (string.IsNullOrEmpty(sku))
                {
                    errors.Add($"Row {row}: SKU is empty");
                    continue;
                }

                var name = headers.ContainsKey("Name")
                    ? worksheet.Cell(row, headers["Name"]).GetString().Trim()
                    : string.Empty;

                decimal price = 0;
                if (headers.ContainsKey("Price"))
                {
                    var cell = worksheet.Cell(row, headers["Price"]);
                    if (cell.DataType == XLDataType.Number)
                    {
                        price = (decimal)cell.GetDouble();
                    }
                    else
                    {
                        var priceStr = cell.GetString().Trim();
                        decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out price);
                    }
                }

                int stock = 0;
                if (headers.ContainsKey("Stock"))
                {
                    var cell = worksheet.Cell(row, headers["Stock"]);
                    if (cell.DataType == XLDataType.Number)
                    {
                        stock = (int)cell.GetDouble();
                    }
                    else
                    {
                        var stockStr = cell.GetString().Trim();
                        int.TryParse(stockStr, out stock);
                    }
                }

                // Validation: negative price
                if (price < 0)
                {
                    errors.Add($"Row {row}: Negative price ({price}) for SKU '{sku}'");
                    continue;
                }

                // Check for duplicate / update existing
                if (existingProducts.ContainsKey(sku))
                {
                    if (updateExisting)
                    {
                        var existing = existingProducts[sku];
                        existing.Name = name;
                        existing.SalePrice = price;
                        existing.Stock = stock;
                        imported.Add(existing);
                    }
                    else
                    {
                        errors.Add($"Row {row}: Duplicate SKU '{sku}' — skipped");
                    }
                    continue;
                }

                var product = new Product
                {
                    SKU = sku,
                    Name = name,
                    SalePrice = price,
                    Stock = stock,
                    CategoryId = Guid.NewGuid(),
                    IsActive = true
                };

                imported.Add(product);
                existingProducts[sku] = product;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {row}: {ex.Message}");
            }
        }

        return (imported, errors);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Valid Excel — all rows imported
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkImport_ValidExcel_ShouldImportAllRows()
    {
        // Arrange
        var headers = new[] { "SKU", "Name", "Price", "Stock" };
        var rows = new List<object[]>
        {
            new object[] { "SKU-001", "Product A", 29.99m, 50 },
            new object[] { "SKU-002", "Product B", 49.50m, 100 },
            new object[] { "SKU-003", "Product C", 15.00m, 25 },
        };

        using var stream = CreateExcelStream(headers, rows);

        // Act
        var (imported, errors) = ParseExcelImport(stream);

        // Assert
        imported.Should().HaveCount(3);
        errors.Should().BeEmpty();

        imported[0].SKU.Should().Be("SKU-001");
        imported[0].Name.Should().Be("Product A");
        imported[0].SalePrice.Should().Be(29.99m);
        imported[0].Stock.Should().Be(50);

        imported[1].SKU.Should().Be("SKU-002");
        imported[2].SKU.Should().Be("SKU-003");
    }

    // ═══════════════════════════════════════════════════════════
    // 2. Duplicate SKU with UpdateExisting=true — updates existing
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkImport_DuplicateSKU_ShouldUpdateExisting()
    {
        // Arrange — existing product with SKU-001
        var existingProduct = new Product
        {
            SKU = "SKU-001",
            Name = "Old Name",
            SalePrice = 10.00m,
            Stock = 5,
            CategoryId = Guid.NewGuid()
        };
        var existingProducts = new Dictionary<string, Product>
        {
            ["SKU-001"] = existingProduct
        };

        var headers = new[] { "SKU", "Name", "Price", "Stock" };
        var rows = new List<object[]>
        {
            new object[] { "SKU-001", "Updated Name", 25.00m, 75 },
        };

        using var stream = CreateExcelStream(headers, rows);

        // Act
        var (imported, errors) = ParseExcelImport(stream, existingProducts, updateExisting: true);

        // Assert
        errors.Should().BeEmpty();
        imported.Should().HaveCount(1);

        // Verify the existing product was updated in-place
        existingProduct.Name.Should().Be("Updated Name");
        existingProduct.SalePrice.Should().Be(25.00m);
        existingProduct.Stock.Should().Be(75);
    }

    // ═══════════════════════════════════════════════════════════
    // 3. Missing required column (SKU) — fails validation
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkImport_MissingRequiredColumn_ShouldFailValidation()
    {
        // Arrange — Excel without "SKU" column
        var headers = new[] { "Name", "Price", "Stock" };
        var rows = new List<object[]>
        {
            new object[] { "Product A", 29.99m, 50 },
        };

        using var stream = CreateExcelStream(headers, rows);

        // Act
        var (imported, errors) = ParseExcelImport(stream);

        // Assert
        imported.Should().BeEmpty();
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("Missing required column: SKU");
    }

    // ═══════════════════════════════════════════════════════════
    // 4. Negative price — reports row error, others succeed
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkImport_NegativePrice_ShouldReportRowError()
    {
        // Arrange
        var headers = new[] { "SKU", "Name", "Price", "Stock" };
        var rows = new List<object[]>
        {
            new object[] { "SKU-001", "Good Product", 29.99m, 50 },
            new object[] { "SKU-002", "Bad Price Product", -10.00m, 20 },
            new object[] { "SKU-003", "Another Good Product", 15.00m, 30 },
        };

        using var stream = CreateExcelStream(headers, rows);

        // Act
        var (imported, errors) = ParseExcelImport(stream);

        // Assert
        imported.Should().HaveCount(2, "rows with valid data should be imported");
        errors.Should().HaveCount(1, "only the negative price row should fail");
        errors[0].Should().Contain("Negative price");
        errors[0].Should().Contain("SKU-002");

        imported.Select(p => p.SKU).Should().Contain("SKU-001");
        imported.Select(p => p.SKU).Should().Contain("SKU-003");
        imported.Select(p => p.SKU).Should().NotContain("SKU-002");
    }

    // ═══════════════════════════════════════════════════════════
    // 5. Performance gate — 10K rows in < 30 seconds
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkImport_10KRows_ShouldCompleteInReasonableTime()
    {
        // Arrange — generate 10,000 rows
        var headers = new[] { "SKU", "Name", "Price", "Stock" };
        var rows = new List<object[]>(10_000);
        for (int i = 0; i < 10_000; i++)
        {
            rows.Add(new object[]
            {
                $"PERF-{i:D5}",
                $"Performance Test Product {i}",
                (decimal)(10.00 + (i * 0.01)),
                i % 500
            });
        }

        using var stream = CreateExcelStream(headers, rows);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var (imported, errors) = ParseExcelImport(stream);
        stopwatch.Stop();

        // Assert
        imported.Should().HaveCount(10_000);
        errors.Should().BeEmpty();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
            "10K row import should complete within 30 seconds (performance gate)");
    }
}
