namespace ApexPerformance.API.Shared.DataTransferObjects.BoxNow;

public record BoxNowDeliveryRequest(
    string OrderNumber,
    string InvoiceValue,
    string PaymentMode,
    string AmountToBeCollected,
    bool AllowReturn,
    Origin Origin,
    Destination Destination,
    List<Item> Items
);

public record Origin(
    string ContactNumber,
    string ContactEmail,
    string ContactName,
    string LocationId
);

public record Destination(
    string ContactNumber,
    string ContactEmail,
    string ContactName,
    string LocationId
);

public record Item(
    string Id,
    string Name,
    string Value,
    double Weight,
    int CompartmentSize
);