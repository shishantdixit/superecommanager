using System.Text.Json.Serialization;

namespace SuperEcomManager.Integrations.Couriers.Delhivery.Models;

#region Order Creation

/// <summary>
/// Request model for creating a Delhivery shipment.
/// </summary>
public class DelhiveryCreateShipmentRequest
{
    [JsonPropertyName("shipments")]
    public List<DelhiveryShipment> Shipments { get; set; } = new();

    [JsonPropertyName("pickup_location")]
    public DelhiveryPickupLocation PickupLocation { get; set; } = new();
}

public class DelhiveryShipment
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("add")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("pin")]
    public string Pincode { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = "India";

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("payment_mode")]
    public string PaymentMode { get; set; } = "Prepaid"; // Prepaid or COD

    [JsonPropertyName("cod_amount")]
    public decimal CodAmount { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("products_desc")]
    public string ProductsDescription { get; set; } = string.Empty;

    [JsonPropertyName("hsn_code")]
    public string? HsnCode { get; set; }

    [JsonPropertyName("seller_name")]
    public string? SellerName { get; set; }

    [JsonPropertyName("seller_add")]
    public string? SellerAddress { get; set; }

    [JsonPropertyName("seller_inv")]
    public string? SellerInvoice { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("waybill")]
    public string? Waybill { get; set; } // Pre-generated waybill if available

    [JsonPropertyName("shipment_width")]
    public decimal? Width { get; set; }

    [JsonPropertyName("shipment_height")]
    public decimal? Height { get; set; }

    [JsonPropertyName("shipment_length")]
    public decimal? Length { get; set; }

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("return_pin")]
    public string? ReturnPincode { get; set; }

    [JsonPropertyName("return_city")]
    public string? ReturnCity { get; set; }

    [JsonPropertyName("return_phone")]
    public string? ReturnPhone { get; set; }

    [JsonPropertyName("return_add")]
    public string? ReturnAddress { get; set; }

    [JsonPropertyName("return_state")]
    public string? ReturnState { get; set; }

    [JsonPropertyName("return_country")]
    public string? ReturnCountry { get; set; }

    [JsonPropertyName("return_name")]
    public string? ReturnName { get; set; }

    [JsonPropertyName("fragile_shipment")]
    public bool? FragileShipment { get; set; }

    [JsonPropertyName("client")]
    public string? Client { get; set; }

    [JsonPropertyName("gst_tin")]
    public string? GstTin { get; set; }

    [JsonPropertyName("extra_parameters")]
    public Dictionary<string, string>? ExtraParameters { get; set; }
}

public class DelhiveryPickupLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("add")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("pin_code")]
    public string Pincode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = "India";

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Response from Delhivery order creation.
/// </summary>
public class DelhiveryCreateShipmentResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("rmk")]
    public string? Remarks { get; set; }

    [JsonPropertyName("packages")]
    public List<DelhiveryPackageResponse>? Packages { get; set; }

    [JsonPropertyName("cash_pickups")]
    public int CashPickups { get; set; }

    [JsonPropertyName("cash_pickups_count")]
    public int CashPickupsCount { get; set; }

    [JsonPropertyName("package_count")]
    public int PackageCount { get; set; }

    [JsonPropertyName("upload_wbn")]
    public string? UploadWaybill { get; set; }

    [JsonPropertyName("cod_count")]
    public int CodCount { get; set; }

    [JsonPropertyName("cod_amount")]
    public decimal CodAmount { get; set; }

    [JsonPropertyName("prepaid_count")]
    public int PrepaidCount { get; set; }

    [JsonPropertyName("prepaid_amount")]
    public decimal PrepaidAmount { get; set; }
}

public class DelhiveryPackageResponse
{
    [JsonPropertyName("waybill")]
    public string Waybill { get; set; } = string.Empty;

    [JsonPropertyName("refnum")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("cod_amount")]
    public decimal CodAmount { get; set; }
}

#endregion

#region Waybill Generation

/// <summary>
/// Request for generating waybills in advance.
/// </summary>
public class DelhiveryWaybillRequest
{
    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
}

/// <summary>
/// Response containing generated waybills.
/// </summary>
public class DelhiveryWaybillResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("waybills")]
    public List<string>? Waybills { get; set; }
}

#endregion

#region Pincode Serviceability

/// <summary>
/// Response for pincode serviceability check.
/// </summary>
public class DelhiveryPincodeResponse
{
    [JsonPropertyName("delivery_codes")]
    public List<DelhiveryDeliveryCode>? DeliveryCodes { get; set; }
}

public class DelhiveryDeliveryCode
{
    [JsonPropertyName("postal_code")]
    public DelhiveryPostalCode? PostalCode { get; set; }
}

public class DelhiveryPostalCode
{
    [JsonPropertyName("pin")]
    public string? Pin { get; set; }

    [JsonPropertyName("max_weight")]
    public decimal? MaxWeight { get; set; }

    [JsonPropertyName("max_amount")]
    public decimal? MaxAmount { get; set; }

    [JsonPropertyName("pre_paid")]
    public string? PrePaid { get; set; }

    [JsonPropertyName("cod")]
    public string? Cod { get; set; }

    [JsonPropertyName("pickup")]
    public string? Pickup { get; set; }

    [JsonPropertyName("repl")]
    public string? Replacement { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("state_code")]
    public string? StateCode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("is_oda")]
    public string? IsOda { get; set; }

    [JsonPropertyName("sort_code")]
    public string? SortCode { get; set; }

    [JsonPropertyName("inc")]
    public string? Inc { get; set; }
}

#endregion

#region Tracking

/// <summary>
/// Response for tracking API.
/// </summary>
public class DelhiveryTrackingResponse
{
    [JsonPropertyName("ShipmentData")]
    public List<DelhiveryShipmentData>? ShipmentData { get; set; }
}

public class DelhiveryShipmentData
{
    [JsonPropertyName("Shipment")]
    public DelhiveryShipmentInfo? Shipment { get; set; }
}

public class DelhiveryShipmentInfo
{
    [JsonPropertyName("AWB")]
    public string? Awb { get; set; }

    [JsonPropertyName("Status")]
    public DelhiveryStatusInfo? Status { get; set; }

    [JsonPropertyName("Scans")]
    public List<DelhiveryScan>? Scans { get; set; }

    [JsonPropertyName("ReferenceNo")]
    public string? ReferenceNo { get; set; }

    [JsonPropertyName("Destination")]
    public string? Destination { get; set; }

    [JsonPropertyName("Origin")]
    public string? Origin { get; set; }

    [JsonPropertyName("CODAmount")]
    public decimal? CodAmount { get; set; }

    [JsonPropertyName("ChargedWeight")]
    public decimal? ChargedWeight { get; set; }

    [JsonPropertyName("ExpectedDeliveryDate")]
    public string? ExpectedDeliveryDate { get; set; }

    [JsonPropertyName("ReturnPromisedDeliveryDate")]
    public string? ReturnPromisedDeliveryDate { get; set; }

    [JsonPropertyName("PromisedDeliveryDate")]
    public string? PromisedDeliveryDate { get; set; }

    [JsonPropertyName("PickUpDate")]
    public string? PickUpDate { get; set; }

    [JsonPropertyName("DispatchCount")]
    public int? DispatchCount { get; set; }

    [JsonPropertyName("OrderType")]
    public string? OrderType { get; set; }

    [JsonPropertyName("SenderName")]
    public string? SenderName { get; set; }

    [JsonPropertyName("Consignee")]
    public DelhiveryConsignee? Consignee { get; set; }
}

public class DelhiveryStatusInfo
{
    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("StatusCode")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("StatusLocation")]
    public string? StatusLocation { get; set; }

    [JsonPropertyName("StatusDateTime")]
    public string? StatusDateTime { get; set; }

    [JsonPropertyName("StatusType")]
    public string? StatusType { get; set; }

    [JsonPropertyName("Instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("RecievedBy")]
    public string? ReceivedBy { get; set; }
}

public class DelhiveryScan
{
    [JsonPropertyName("ScanDetail")]
    public DelhiveryScanDetail? ScanDetail { get; set; }
}

public class DelhiveryScanDetail
{
    [JsonPropertyName("Scan")]
    public string? Scan { get; set; }

    [JsonPropertyName("ScanDateTime")]
    public string? ScanDateTime { get; set; }

    [JsonPropertyName("ScanType")]
    public string? ScanType { get; set; }

    [JsonPropertyName("ScannedLocation")]
    public string? ScannedLocation { get; set; }

    [JsonPropertyName("Instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("StatusCode")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("StatusDateTime")]
    public string? StatusDateTime { get; set; }
}

public class DelhiveryConsignee
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("Address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("Address3")]
    public string? Address3 { get; set; }

    [JsonPropertyName("City")]
    public string? City { get; set; }

    [JsonPropertyName("State")]
    public string? State { get; set; }

    [JsonPropertyName("PinCode")]
    public string? PinCode { get; set; }

    [JsonPropertyName("Telephone1")]
    public string? Telephone1 { get; set; }

    [JsonPropertyName("Telephone2")]
    public string? Telephone2 { get; set; }
}

#endregion

#region Pickup

/// <summary>
/// Request for scheduling a pickup.
/// </summary>
public class DelhiveryPickupRequest
{
    [JsonPropertyName("pickup_time")]
    public string PickupTime { get; set; } = string.Empty;

    [JsonPropertyName("pickup_date")]
    public string PickupDate { get; set; } = string.Empty;

    [JsonPropertyName("pickup_location")]
    public string PickupLocation { get; set; } = string.Empty;

    [JsonPropertyName("expected_package_count")]
    public int ExpectedPackageCount { get; set; }
}

/// <summary>
/// Response from pickup request.
/// </summary>
public class DelhiveryPickupResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("pickup_id")]
    public string? PickupId { get; set; }

    [JsonPropertyName("pickup_date")]
    public string? PickupDate { get; set; }

    [JsonPropertyName("pickup_time")]
    public string? PickupTime { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

#endregion

#region Cancellation

/// <summary>
/// Request to cancel an order.
/// </summary>
public class DelhiveryCancelRequest
{
    [JsonPropertyName("waybill")]
    public string Waybill { get; set; } = string.Empty;

    [JsonPropertyName("cancellation")]
    public bool Cancellation { get; set; } = true;
}

/// <summary>
/// Response from cancel request.
/// </summary>
public class DelhiveryCancelResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("waybill")]
    public string? Waybill { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

#endregion

#region Rate Calculator

/// <summary>
/// Request for rate calculation.
/// </summary>
public class DelhiveryRateRequest
{
    [JsonPropertyName("md")]
    public string Mode { get; set; } = "S"; // S for Surface, E for Express

    [JsonPropertyName("ss")]
    public string SourceState { get; set; } = string.Empty;

    [JsonPropertyName("d")]
    public string DestinationPincode { get; set; } = string.Empty;

    [JsonPropertyName("o")]
    public string OriginPincode { get; set; } = string.Empty;

    [JsonPropertyName("cgm")]
    public decimal ChargedWeightGrams { get; set; }

    [JsonPropertyName("pt")]
    public string PaymentType { get; set; } = "Pre-paid"; // Pre-paid or COD

    [JsonPropertyName("cod")]
    public decimal? CodAmount { get; set; }
}

/// <summary>
/// Response from rate calculation.
/// </summary>
public class DelhiveryRateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("charges")]
    public DelhiveryCharges? Charges { get; set; }
}

public class DelhiveryCharges
{
    [JsonPropertyName("freight_charge")]
    public decimal FreightCharge { get; set; }

    [JsonPropertyName("cod_charge")]
    public decimal CodCharge { get; set; }

    [JsonPropertyName("fuel_surcharge")]
    public decimal FuelSurcharge { get; set; }

    [JsonPropertyName("handling_charge")]
    public decimal HandlingCharge { get; set; }

    [JsonPropertyName("docket_charge")]
    public decimal DocketCharge { get; set; }

    [JsonPropertyName("green_tax")]
    public decimal GreenTax { get; set; }

    [JsonPropertyName("gst")]
    public decimal Gst { get; set; }
}

#endregion

#region Webhook

/// <summary>
/// Delhivery webhook payload.
/// </summary>
public class DelhiveryWebhookPayload
{
    [JsonPropertyName("waybill")]
    public string? Waybill { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("status_code")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("reference_number")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("delivered_to")]
    public string? DeliveredTo { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("received_by")]
    public string? ReceivedBy { get; set; }

    [JsonPropertyName("pod")]
    public string? Pod { get; set; }
}

#endregion
