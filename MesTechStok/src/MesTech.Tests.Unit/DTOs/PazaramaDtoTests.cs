using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Tests.Unit.DTOs;

[Trait("Category", "Unit")]
[Trait("Phase", "Dalga4")]
public class PazaramaDtoTests
{
    private static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void PzApiResponse_Deserializes_Success()
    {
        // Arrange
        var json = """
        {
            "data": { "name": "Test Product", "id": "00000000-0000-0000-0000-000000000001" },
            "success": true,
            "messageCode": "200",
            "message": "OK",
            "userMessage": null,
            "fromCache": false
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<PzApiResponse<PzProduct>>(json, CamelCase);

        // Assert
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.MessageCode.Should().Be("200");
        response.Message.Should().Be("OK");
        response.UserMessage.Should().BeNull();
        response.FromCache.Should().BeFalse();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Test Product");
    }

    [Fact]
    public void PzBatchResultResponse_Status_Maps_Correctly()
    {
        // Arrange
        var json = """
        {
            "status": 2,
            "batchResult": [
                { "code": "PRD-001", "isSuccess": true, "message": null },
                { "code": "PRD-002", "isSuccess": false, "message": "Invalid barcode" }
            ],
            "totalCount": 2,
            "failedCount": 1
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<PzBatchResultResponse>(json, CamelCase);

        // Assert — Status 2 = Done
        result.Should().NotBeNull();
        result!.Status.Should().Be(2);
        result.TotalCount.Should().Be(2);
        result.FailedCount.Should().Be(1);
        result.BatchResult.Should().HaveCount(2);
        result.BatchResult[0].IsSuccess.Should().BeTrue();
        result.BatchResult[1].IsSuccess.Should().BeFalse();
        result.BatchResult[1].Message.Should().Be("Invalid barcode");
    }

    [Fact]
    public void PzOrder_Deserializes_With_Items()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var json = $$"""
        {
            "orderId": "{{orderId}}",
            "orderNumber": 1234567890,
            "orderDate": "2026-03-08T10:30:00",
            "orderAmount": 199.99,
            "orderStatus": 1,
            "customerId": "{{customerId}}",
            "customerName": "Ahmet Yilmaz",
            "shipmentAddress": {
                "fullName": "Ahmet Yilmaz",
                "address": "Ataturk Cad. No:1",
                "city": "Istanbul",
                "district": "Kadikoy",
                "postalCode": "34710",
                "phone": "05551234567"
            },
            "billingAddress": null,
            "items": [
                {
                    "orderItemId": "{{itemId}}",
                    "orderItemStatus": 1,
                    "quantity": 2,
                    "listPrice": 120.00,
                    "salePrice": 99.99,
                    "taxAmount": 18.00,
                    "totalPrice": 199.98,
                    "cargo": {
                        "companyName": "Yurtici Kargo",
                        "trackingNumber": "YK-12345678",
                        "trackingUrl": "https://track.yurticikargo.com/YK-12345678"
                    },
                    "product": {
                        "productId": "{{productId}}",
                        "name": "Wireless Mouse",
                        "code": "WM-001",
                        "vatRate": 20.0,
                        "imageURL": "https://cdn.pazarama.com/img/wm001.jpg"
                    },
                    "deliveryType": 1,
                    "shipmentCode": "SHP-001"
                }
            ]
        }
        """;

        // Act
        var order = JsonSerializer.Deserialize<PzOrder>(json, CamelCase);

        // Assert
        order.Should().NotBeNull();
        order!.OrderId.Should().Be(orderId);
        order.OrderNumber.Should().Be(1234567890);
        order.OrderAmount.Should().Be(199.99m);
        order.CustomerName.Should().Be("Ahmet Yilmaz");

        order.ShipmentAddress.Should().NotBeNull();
        order.ShipmentAddress!.City.Should().Be("Istanbul");
        order.ShipmentAddress.District.Should().Be("Kadikoy");
        order.BillingAddress.Should().BeNull();

        order.Items.Should().HaveCount(1);
        var item = order.Items[0];
        item.OrderItemId.Should().Be(itemId);
        item.Quantity.Should().Be(2);
        item.TotalPrice.Should().Be(199.98m);

        item.Cargo.Should().NotBeNull();
        item.Cargo!.CompanyName.Should().Be("Yurtici Kargo");
        item.Cargo.TrackingNumber.Should().Be("YK-12345678");

        item.Product.Should().NotBeNull();
        item.Product!.ProductId.Should().Be(productId);
        item.Product.Code.Should().Be("WM-001");
    }

    [Fact]
    public void PzRefund_RefundStatus_OnlyFirmValues()
    {
        // Firms can only send status 2 (Onay/Approve) and 3 (Ret/Reject)
        var approveRequest = new PzUpdateRefundRequest
        {
            RefundId = Guid.NewGuid(),
            Status = 2
        };

        var rejectRequest = new PzUpdateRefundRequest
        {
            RefundId = Guid.NewGuid(),
            Status = 3
        };

        // Assert valid firm statuses
        approveRequest.Status.Should().Be(2, "2 = Onay (Approve)");
        rejectRequest.Status.Should().Be(3, "3 = Ret (Reject)");

        // Verify serialization round-trip preserves status
        var approveJson = JsonSerializer.Serialize(approveRequest, CamelCase);
        var approveDeserialized = JsonSerializer.Deserialize<PzUpdateRefundRequest>(approveJson, CamelCase);
        approveDeserialized!.Status.Should().Be(2);

        var rejectJson = JsonSerializer.Serialize(rejectRequest, CamelCase);
        var rejectDeserialized = JsonSerializer.Deserialize<PzUpdateRefundRequest>(rejectJson, CamelCase);
        rejectDeserialized!.Status.Should().Be(3);
    }

    [Fact]
    public void PzProductCreateRequest_Serializes_Batch()
    {
        // Arrange
        var request = new PzProductCreateRequest
        {
            Products = new List<PzProductDetail>
            {
                new()
                {
                    Name = "Product A",
                    DisplayName = "Product A Display",
                    Code = "PRD-A",
                    ListPrice = 100.00m,
                    SalePrice = 89.99m,
                    StockCount = 50,
                    BrandId = Guid.NewGuid(),
                    CategoryId = Guid.NewGuid(),
                    Barcode = "8690000000001",
                    VatRate = 20m,
                    DeliveryType = 1,
                    Attributes = new List<PzAttribute>
                    {
                        new() { Id = Guid.NewGuid(), Name = "Renk", Value = "Kirmizi" }
                    },
                    Images = new List<PzImage>
                    {
                        new() { Url = "https://cdn.example.com/a.jpg", IsMain = true },
                        new() { Url = "https://cdn.example.com/a2.jpg", IsMain = false }
                    }
                },
                new()
                {
                    Name = "Product B",
                    DisplayName = "Product B Display",
                    Code = "PRD-B",
                    ListPrice = 200.00m,
                    SalePrice = 179.99m,
                    StockCount = 30,
                    BrandId = Guid.NewGuid(),
                    CategoryId = Guid.NewGuid()
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, CamelCase);
        var deserialized = JsonSerializer.Deserialize<PzProductCreateRequest>(json, CamelCase);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Products.Should().HaveCount(2);

        var first = deserialized.Products[0];
        first.Name.Should().Be("Product A");
        first.Code.Should().Be("PRD-A");
        first.ListPrice.Should().Be(100.00m);
        first.SalePrice.Should().Be(89.99m);
        first.Barcode.Should().Be("8690000000001");
        first.Attributes.Should().HaveCount(1);
        first.Attributes[0].Name.Should().Be("Renk");
        first.Images.Should().HaveCount(2);
        first.Images[0].IsMain.Should().BeTrue();

        var second = deserialized.Products[1];
        second.Name.Should().Be("Product B");
        second.StockCount.Should().Be(30);
    }

    [Fact]
    public void PzOrderListRequest_DateRange_Defaults()
    {
        // Arrange
        var request = new PzOrderListRequest();

        // Assert — default page size is 50, page number is 1
        request.PageSize.Should().Be(50);
        request.PageNumber.Should().Be(1);
        request.OrderNumber.Should().BeNull();

        // Set a valid date range (max 1 month)
        var startDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        request.StartDate = startDate;
        request.EndDate = endDate;

        var rangeDays = (request.EndDate - request.StartDate).TotalDays;
        rangeDays.Should().BeLessThanOrEqualTo(31, "Pazarama date range max 1 month");

        // Verify serialization round-trip
        var json = JsonSerializer.Serialize(request, CamelCase);
        var deserialized = JsonSerializer.Deserialize<PzOrderListRequest>(json, CamelCase);
        deserialized.Should().NotBeNull();
        deserialized!.PageSize.Should().Be(50);
        deserialized.StartDate.Should().Be(startDate);
        deserialized.EndDate.Should().Be(endDate);
    }
}
