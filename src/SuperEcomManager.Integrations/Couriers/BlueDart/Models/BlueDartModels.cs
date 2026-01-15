using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace SuperEcomManager.Integrations.Couriers.BlueDart.Models;

#region Authentication

/// <summary>
/// BlueDart uses license key authentication.
/// </summary>
public class BlueDartProfile
{
    [JsonPropertyName("LoginID")]
    public string LoginId { get; set; } = string.Empty;

    [JsonPropertyName("LicenceKey")]
    public string LicenseKey { get; set; } = string.Empty;

    [JsonPropertyName("Api_type")]
    public string ApiType { get; set; } = "S"; // S for Softcom
}

#endregion

#region Waybill Generation

/// <summary>
/// Request for generating waybill.
/// </summary>
public class BlueDartWaybillRequest
{
    [JsonPropertyName("Request")]
    public BlueDartWaybillRequestData Request { get; set; } = new();
}

public class BlueDartWaybillRequestData
{
    [JsonPropertyName("Consignee")]
    public BlueDartConsignee Consignee { get; set; } = new();

    [JsonPropertyName("Services")]
    public BlueDartServices Services { get; set; } = new();

    [JsonPropertyName("Shipper")]
    public BlueDartShipper Shipper { get; set; } = new();
}

public class BlueDartConsignee
{
    [JsonPropertyName("ConsigneeName")]
    public string ConsigneeName { get; set; } = string.Empty;

    [JsonPropertyName("ConsigneeAddress1")]
    public string Address1 { get; set; } = string.Empty;

    [JsonPropertyName("ConsigneeAddress2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("ConsigneeAddress3")]
    public string? Address3 { get; set; }

    [JsonPropertyName("ConsigneePincode")]
    public string Pincode { get; set; } = string.Empty;

    [JsonPropertyName("ConsigneeMobile")]
    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("ConsigneeTelephone")]
    public string? Telephone { get; set; }

    [JsonPropertyName("ConsigneeEmailID")]
    public string? Email { get; set; }

    [JsonPropertyName("ConsigneeAttention")]
    public string? Attention { get; set; }
}

public class BlueDartServices
{
    [JsonPropertyName("ActualWeight")]
    public decimal ActualWeight { get; set; }

    [JsonPropertyName("CollectableAmount")]
    public decimal CollectableAmount { get; set; }

    [JsonPropertyName("Commodity")]
    public BlueDartCommodity Commodity { get; set; } = new();

    [JsonPropertyName("CreditReferenceNo")]
    public string? CreditReferenceNo { get; set; }

    [JsonPropertyName("CreditReferenceNo2")]
    public string? CreditReferenceNo2 { get; set; }

    [JsonPropertyName("CreditReferenceNo3")]
    public string? CreditReferenceNo3 { get; set; }

    [JsonPropertyName("DeclaredValue")]
    public decimal DeclaredValue { get; set; }

    [JsonPropertyName("Dimensions")]
    public BlueDartDimensions? Dimensions { get; set; }

    [JsonPropertyName("InvoiceNo")]
    public string? InvoiceNo { get; set; }

    [JsonPropertyName("ItemCount")]
    public int ItemCount { get; set; } = 1;

    [JsonPropertyName("PickupDate")]
    public string PickupDate { get; set; } = string.Empty;

    [JsonPropertyName("PickupTime")]
    public string PickupTime { get; set; } = "1000";

    [JsonPropertyName("PieceCount")]
    public int PieceCount { get; set; } = 1;

    [JsonPropertyName("ProductCode")]
    public string ProductCode { get; set; } = "A"; // A = Air, D = Apex (Ground)

    [JsonPropertyName("ProductType")]
    public int ProductType { get; set; } = 2; // 1 = Docs, 2 = Non-Docs

    [JsonPropertyName("SpecialInstruction")]
    public string? SpecialInstruction { get; set; }

    [JsonPropertyName("SubProductCode")]
    public string SubProductCode { get; set; } = "P"; // P = Priority
}

public class BlueDartCommodity
{
    [JsonPropertyName("CommodityDetail1")]
    public string Detail1 { get; set; } = string.Empty;

    [JsonPropertyName("CommodityDetail2")]
    public string? Detail2 { get; set; }

    [JsonPropertyName("CommodityDetail3")]
    public string? Detail3 { get; set; }
}

public class BlueDartDimensions
{
    [JsonPropertyName("Dimension")]
    public List<BlueDartDimension> DimensionList { get; set; } = new();
}

public class BlueDartDimension
{
    [JsonPropertyName("Breadth")]
    public decimal Breadth { get; set; }

    [JsonPropertyName("Count")]
    public int Count { get; set; } = 1;

    [JsonPropertyName("Height")]
    public decimal Height { get; set; }

    [JsonPropertyName("Length")]
    public decimal Length { get; set; }
}

public class BlueDartShipper
{
    [JsonPropertyName("CustomerAddress1")]
    public string Address1 { get; set; } = string.Empty;

    [JsonPropertyName("CustomerAddress2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("CustomerAddress3")]
    public string? Address3 { get; set; }

    [JsonPropertyName("CustomerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    [JsonPropertyName("CustomerEmailID")]
    public string? Email { get; set; }

    [JsonPropertyName("CustomerMobile")]
    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("CustomerName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("CustomerPincode")]
    public string Pincode { get; set; } = string.Empty;

    [JsonPropertyName("CustomerTelephone")]
    public string? Telephone { get; set; }

    [JsonPropertyName("IsToPayCustomer")]
    public bool IsToPayCustomer { get; set; }

    [JsonPropertyName("OriginArea")]
    public string OriginArea { get; set; } = string.Empty;

    [JsonPropertyName("Sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("VendorCode")]
    public string? VendorCode { get; set; }
}

/// <summary>
/// Response from waybill generation.
/// </summary>
public class BlueDartWaybillResponse
{
    [JsonPropertyName("GenerateWaybillResult")]
    public BlueDartWaybillResult? Result { get; set; }
}

public class BlueDartWaybillResult
{
    [JsonPropertyName("AWBNo")]
    public string? AwbNo { get; set; }

    [JsonPropertyName("DestinationArea")]
    public string? DestinationArea { get; set; }

    [JsonPropertyName("DestinationLocation")]
    public string? DestinationLocation { get; set; }

    [JsonPropertyName("ErrorMessage")]
    public List<string>? ErrorMessages { get; set; }

    [JsonPropertyName("IsError")]
    public bool IsError { get; set; }

    [JsonPropertyName("Status")]
    public int Status { get; set; }
}

#endregion

#region Pincode Serviceability

/// <summary>
/// Request for pincode serviceability check.
/// </summary>
public class BlueDartPincodeRequest
{
    [JsonPropertyName("pinCode")]
    public string PinCode { get; set; } = string.Empty;
}

/// <summary>
/// Response for pincode serviceability.
/// </summary>
public class BlueDartPincodeResponse
{
    [JsonPropertyName("GetServicesforPincodeResult")]
    public BlueDartPincodeResult? Result { get; set; }
}

public class BlueDartPincodeResult
{
    [JsonPropertyName("AreaCode")]
    public List<string>? AreaCodes { get; set; }

    [JsonPropertyName("CityCode")]
    public string? CityCode { get; set; }

    [JsonPropertyName("CityName")]
    public string? CityName { get; set; }

    [JsonPropertyName("CountryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("ErrorMessage")]
    public List<string>? ErrorMessages { get; set; }

    [JsonPropertyName("IsError")]
    public bool IsError { get; set; }

    [JsonPropertyName("StateCode")]
    public string? StateCode { get; set; }

    [JsonPropertyName("StateName")]
    public string? StateName { get; set; }

    [JsonPropertyName("ApexInbound")]
    public string? ApexInbound { get; set; }

    [JsonPropertyName("ApexOutbound")]
    public string? ApexOutbound { get; set; }

    [JsonPropertyName("AvailableServiceCodes")]
    public List<string>? AvailableServiceCodes { get; set; }
}

#endregion

#region Tracking

/// <summary>
/// Request for tracking.
/// </summary>
public class BlueDartTrackingRequest
{
    [JsonPropertyName("AWBNo")]
    public string AwbNo { get; set; } = string.Empty;
}

/// <summary>
/// Response for tracking.
/// </summary>
public class BlueDartTrackingResponse
{
    [JsonPropertyName("GetShipmentTrackingResult")]
    public BlueDartTrackingResult? Result { get; set; }
}

public class BlueDartTrackingResult
{
    [JsonPropertyName("ErrorMessage")]
    public List<string>? ErrorMessages { get; set; }

    [JsonPropertyName("IsError")]
    public bool IsError { get; set; }

    [JsonPropertyName("ShipmentTrackingDetails")]
    public List<BlueDartTrackingDetail>? TrackingDetails { get; set; }
}

public class BlueDartTrackingDetail
{
    [JsonPropertyName("AWBNumber")]
    public string? AwbNumber { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("StatusDate")]
    public string? StatusDate { get; set; }

    [JsonPropertyName("StatusTime")]
    public string? StatusTime { get; set; }

    [JsonPropertyName("StatusType")]
    public string? StatusType { get; set; }

    [JsonPropertyName("StatusLocation")]
    public string? StatusLocation { get; set; }

    [JsonPropertyName("Instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("ReceivedBy")]
    public string? ReceivedBy { get; set; }

    [JsonPropertyName("Remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("ExpectedDeliveryDate")]
    public string? ExpectedDeliveryDate { get; set; }
}

#endregion

#region Pickup

/// <summary>
/// Request for scheduling pickup.
/// </summary>
public class BlueDartPickupRequest
{
    [JsonPropertyName("AreaCode")]
    public string AreaCode { get; set; } = string.Empty;

    [JsonPropertyName("CustomerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    [JsonPropertyName("CustomerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("CustomerAddress1")]
    public string Address1 { get; set; } = string.Empty;

    [JsonPropertyName("CustomerAddress2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("CustomerPincode")]
    public string Pincode { get; set; } = string.Empty;

    [JsonPropertyName("CustomerMobile")]
    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("PickupDate")]
    public string PickupDate { get; set; } = string.Empty;

    [JsonPropertyName("PickupTime")]
    public string PickupTime { get; set; } = "1400";

    [JsonPropertyName("ReadyTime")]
    public string ReadyTime { get; set; } = "1000";

    [JsonPropertyName("CloseTime")]
    public string CloseTime { get; set; } = "1800";

    [JsonPropertyName("ProductType")]
    public int ProductType { get; set; } = 2;

    [JsonPropertyName("NumberOfPieces")]
    public int NumberOfPieces { get; set; }

    [JsonPropertyName("ActualWeight")]
    public decimal ActualWeight { get; set; }

    [JsonPropertyName("SpecialInstruction")]
    public string? SpecialInstruction { get; set; }
}

/// <summary>
/// Response for pickup scheduling.
/// </summary>
public class BlueDartPickupResponse
{
    [JsonPropertyName("RegisterPickupResult")]
    public BlueDartPickupResult? Result { get; set; }
}

public class BlueDartPickupResult
{
    [JsonPropertyName("ErrorMessage")]
    public List<string>? ErrorMessages { get; set; }

    [JsonPropertyName("IsError")]
    public bool IsError { get; set; }

    [JsonPropertyName("PickupRegistrationNumber")]
    public string? PickupRegistrationNumber { get; set; }

    [JsonPropertyName("TokenNumber")]
    public string? TokenNumber { get; set; }
}

#endregion

#region Cancel

/// <summary>
/// Request for cancellation.
/// </summary>
public class BlueDartCancelRequest
{
    [JsonPropertyName("AWBNo")]
    public string AwbNo { get; set; } = string.Empty;

    [JsonPropertyName("CancellationReason")]
    public string CancellationReason { get; set; } = "Customer Request";
}

/// <summary>
/// Response for cancellation.
/// </summary>
public class BlueDartCancelResponse
{
    [JsonPropertyName("CancelWaybillResult")]
    public BlueDartCancelResult? Result { get; set; }
}

public class BlueDartCancelResult
{
    [JsonPropertyName("ErrorMessage")]
    public List<string>? ErrorMessages { get; set; }

    [JsonPropertyName("IsError")]
    public bool IsError { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }
}

#endregion

#region Webhook

/// <summary>
/// BlueDart webhook/push notification payload.
/// </summary>
public class BlueDartWebhookPayload
{
    [JsonPropertyName("AWBNo")]
    public string? AwbNo { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("StatusCode")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("StatusDate")]
    public string? StatusDate { get; set; }

    [JsonPropertyName("StatusTime")]
    public string? StatusTime { get; set; }

    [JsonPropertyName("StatusLocation")]
    public string? StatusLocation { get; set; }

    [JsonPropertyName("ReferenceNo")]
    public string? ReferenceNo { get; set; }

    [JsonPropertyName("ReceivedBy")]
    public string? ReceivedBy { get; set; }

    [JsonPropertyName("Remarks")]
    public string? Remarks { get; set; }
}

#endregion
