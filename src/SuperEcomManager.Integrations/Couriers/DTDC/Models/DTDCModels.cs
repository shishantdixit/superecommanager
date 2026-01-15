using System.Text.Json.Serialization;

namespace SuperEcomManager.Integrations.Couriers.DTDC.Models;

#region Authentication

/// <summary>
/// DTDC API credentials.
/// </summary>
public class DTDCCredentials
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("customer_code")]
    public string CustomerCode { get; set; } = string.Empty;
}

#endregion

#region Shipment Creation

/// <summary>
/// Request for creating a DTDC shipment.
/// </summary>
public class DTDCCreateShipmentRequest
{
    [JsonPropertyName("customerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    [JsonPropertyName("consignmentDetails")]
    public List<DTDCConsignment> ConsignmentDetails { get; set; } = new();
}

public class DTDCConsignment
{
    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerReferenceNumber")]
    public string CustomerReferenceNumber { get; set; } = string.Empty;

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = "PREMIUM"; // PREMIUM, EXPRESS, GROUND

    [JsonPropertyName("loadType")]
    public string LoadType { get; set; } = "NON-DOCUMENT";

    [JsonPropertyName("dimension")]
    public DTDCDimension? Dimension { get; set; }

    [JsonPropertyName("noOfPieces")]
    public int NoOfPieces { get; set; } = 1;

    [JsonPropertyName("actualWeight")]
    public decimal ActualWeight { get; set; }

    [JsonPropertyName("codAmount")]
    public decimal CodAmount { get; set; }

    [JsonPropertyName("declaredValue")]
    public decimal DeclaredValue { get; set; }

    [JsonPropertyName("productDescription")]
    public string ProductDescription { get; set; } = string.Empty;

    [JsonPropertyName("consignorDetails")]
    public DTDCPartyDetails ConsignorDetails { get; set; } = new();

    [JsonPropertyName("consigneeDetails")]
    public DTDCPartyDetails ConsigneeDetails { get; set; } = new();

    [JsonPropertyName("returnAddressDetails")]
    public DTDCPartyDetails? ReturnAddressDetails { get; set; }

    [JsonPropertyName("isCOD")]
    public bool IsCOD { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("invoiceDate")]
    public string? InvoiceDate { get; set; }

    [JsonPropertyName("gstNumber")]
    public string? GstNumber { get; set; }
}

public class DTDCDimension
{
    [JsonPropertyName("length")]
    public decimal Length { get; set; }

    [JsonPropertyName("width")]
    public decimal Width { get; set; }

    [JsonPropertyName("height")]
    public decimal Height { get; set; }
}

public class DTDCPartyDetails
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address1")]
    public string Address1 { get; set; } = string.Empty;

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("address3")]
    public string? Address3 { get; set; }

    [JsonPropertyName("pincode")]
    public string Pincode { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; } = "INDIA";

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("mobileNo")]
    public string MobileNo { get; set; } = string.Empty;

    [JsonPropertyName("emailId")]
    public string? EmailId { get; set; }
}

/// <summary>
/// Response from shipment creation.
/// </summary>
public class DTDCCreateShipmentResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public DTDCShipmentData? Data { get; set; }
}

public class DTDCShipmentData
{
    [JsonPropertyName("consignmentNumbers")]
    public List<DTDCConsignmentResult>? ConsignmentNumbers { get; set; }
}

public class DTDCConsignmentResult
{
    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("consignmentNumber")]
    public string? ConsignmentNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

#endregion

#region Pincode Serviceability

/// <summary>
/// Response for pincode serviceability.
/// </summary>
public class DTDCPincodeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public DTDCPincodeData? Data { get; set; }
}

public class DTDCPincodeData
{
    [JsonPropertyName("pincode")]
    public string? Pincode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("serviceable")]
    public bool Serviceable { get; set; }

    [JsonPropertyName("codAvailable")]
    public bool CodAvailable { get; set; }

    [JsonPropertyName("prepaidAvailable")]
    public bool PrepaidAvailable { get; set; }

    [JsonPropertyName("services")]
    public List<DTDCService>? Services { get; set; }
}

public class DTDCService
{
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("serviceCode")]
    public string? ServiceCode { get; set; }

    [JsonPropertyName("deliveryDays")]
    public int DeliveryDays { get; set; }
}

#endregion

#region Tracking

/// <summary>
/// Response for tracking.
/// </summary>
public class DTDCTrackingResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public DTDCTrackingData? Data { get; set; }
}

public class DTDCTrackingData
{
    [JsonPropertyName("consignmentNumber")]
    public string? ConsignmentNumber { get; set; }

    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("currentStatus")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("currentStatusCode")]
    public string? CurrentStatusCode { get; set; }

    [JsonPropertyName("currentLocation")]
    public string? CurrentLocation { get; set; }

    [JsonPropertyName("expectedDeliveryDate")]
    public string? ExpectedDeliveryDate { get; set; }

    [JsonPropertyName("deliveredDate")]
    public string? DeliveredDate { get; set; }

    [JsonPropertyName("receivedBy")]
    public string? ReceivedBy { get; set; }

    [JsonPropertyName("trackingHistory")]
    public List<DTDCTrackingEvent>? TrackingHistory { get; set; }
}

public class DTDCTrackingEvent
{
    [JsonPropertyName("eventDate")]
    public string? EventDate { get; set; }

    [JsonPropertyName("eventTime")]
    public string? EventTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("statusCode")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

#endregion

#region Pickup

/// <summary>
/// Request for scheduling pickup.
/// </summary>
public class DTDCPickupRequest
{
    [JsonPropertyName("customerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    [JsonPropertyName("pickupDate")]
    public string PickupDate { get; set; } = string.Empty;

    [JsonPropertyName("pickupTime")]
    public string PickupTime { get; set; } = "10:00";

    [JsonPropertyName("closingTime")]
    public string ClosingTime { get; set; } = "18:00";

    [JsonPropertyName("consignmentCount")]
    public int ConsignmentCount { get; set; }

    [JsonPropertyName("totalWeight")]
    public decimal TotalWeight { get; set; }

    [JsonPropertyName("pickupAddress")]
    public DTDCPartyDetails PickupAddress { get; set; } = new();

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

/// <summary>
/// Response for pickup scheduling.
/// </summary>
public class DTDCPickupResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public DTDCPickupData? Data { get; set; }
}

public class DTDCPickupData
{
    [JsonPropertyName("pickupRequestNumber")]
    public string? PickupRequestNumber { get; set; }

    [JsonPropertyName("tokenNumber")]
    public string? TokenNumber { get; set; }

    [JsonPropertyName("scheduledDate")]
    public string? ScheduledDate { get; set; }

    [JsonPropertyName("scheduledTime")]
    public string? ScheduledTime { get; set; }
}

#endregion

#region Cancel

/// <summary>
/// Request for cancellation.
/// </summary>
public class DTDCCancelRequest
{
    [JsonPropertyName("customerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    [JsonPropertyName("consignmentNumber")]
    public string ConsignmentNumber { get; set; } = string.Empty;

    [JsonPropertyName("cancellationReason")]
    public string CancellationReason { get; set; } = "Customer Request";
}

/// <summary>
/// Response for cancellation.
/// </summary>
public class DTDCCancelResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public DTDCCancelData? Data { get; set; }
}

public class DTDCCancelData
{
    [JsonPropertyName("consignmentNumber")]
    public string? ConsignmentNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

#endregion

#region Rate Calculator

/// <summary>
/// Request for rate calculation.
/// </summary>
public class DTDCRateRequest
{
    [JsonPropertyName("originPincode")]
    public string OriginPincode { get; set; } = string.Empty;

    [JsonPropertyName("destinationPincode")]
    public string DestinationPincode { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("codAmount")]
    public decimal CodAmount { get; set; }

    [JsonPropertyName("declaredValue")]
    public decimal DeclaredValue { get; set; }

    [JsonPropertyName("loadType")]
    public string LoadType { get; set; } = "NON-DOCUMENT";
}

/// <summary>
/// Response for rate calculation.
/// </summary>
public class DTDCRateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public List<DTDCRateData>? Data { get; set; }
}

public class DTDCRateData
{
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("serviceCode")]
    public string? ServiceCode { get; set; }

    [JsonPropertyName("freightCharge")]
    public decimal FreightCharge { get; set; }

    [JsonPropertyName("codCharge")]
    public decimal CodCharge { get; set; }

    [JsonPropertyName("fuelSurcharge")]
    public decimal FuelSurcharge { get; set; }

    [JsonPropertyName("handlingCharge")]
    public decimal HandlingCharge { get; set; }

    [JsonPropertyName("gst")]
    public decimal Gst { get; set; }

    [JsonPropertyName("totalCharge")]
    public decimal TotalCharge { get; set; }

    [JsonPropertyName("deliveryDays")]
    public int DeliveryDays { get; set; }
}

#endregion

#region Webhook

/// <summary>
/// DTDC webhook payload.
/// </summary>
public class DTDCWebhookPayload
{
    [JsonPropertyName("consignmentNumber")]
    public string? ConsignmentNumber { get; set; }

    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("statusCode")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("eventDate")]
    public string? EventDate { get; set; }

    [JsonPropertyName("eventTime")]
    public string? EventTime { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("receivedBy")]
    public string? ReceivedBy { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

#endregion
