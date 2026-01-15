using System.Text.Json.Serialization;

namespace SuperEcomManager.Integrations.Couriers.Shiprocket.Models;

#region Authentication

public class ShiprocketAuthRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class ShiprocketAuthResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("company_id")]
    public int CompanyId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

#endregion

#region Orders

public class ShiprocketCreateOrderRequest
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("order_date")]
    public string OrderDate { get; set; } = string.Empty;

    [JsonPropertyName("pickup_location")]
    public string PickupLocation { get; set; } = string.Empty;

    [JsonPropertyName("channel_id")]
    public int? ChannelId { get; set; }

    [JsonPropertyName("billing_customer_name")]
    public string BillingCustomerName { get; set; } = string.Empty;

    [JsonPropertyName("billing_last_name")]
    public string? BillingLastName { get; set; }

    [JsonPropertyName("billing_address")]
    public string BillingAddress { get; set; } = string.Empty;

    [JsonPropertyName("billing_address_2")]
    public string? BillingAddress2 { get; set; }

    [JsonPropertyName("billing_city")]
    public string BillingCity { get; set; } = string.Empty;

    [JsonPropertyName("billing_pincode")]
    public string BillingPincode { get; set; } = string.Empty;

    [JsonPropertyName("billing_state")]
    public string BillingState { get; set; } = string.Empty;

    [JsonPropertyName("billing_country")]
    public string BillingCountry { get; set; } = "India";

    [JsonPropertyName("billing_email")]
    public string? BillingEmail { get; set; }

    [JsonPropertyName("billing_phone")]
    public string BillingPhone { get; set; } = string.Empty;

    [JsonPropertyName("shipping_is_billing")]
    public bool ShippingIsBilling { get; set; } = true;

    [JsonPropertyName("shipping_customer_name")]
    public string? ShippingCustomerName { get; set; }

    [JsonPropertyName("shipping_last_name")]
    public string? ShippingLastName { get; set; }

    [JsonPropertyName("shipping_address")]
    public string? ShippingAddress { get; set; }

    [JsonPropertyName("shipping_address_2")]
    public string? ShippingAddress2 { get; set; }

    [JsonPropertyName("shipping_city")]
    public string? ShippingCity { get; set; }

    [JsonPropertyName("shipping_pincode")]
    public string? ShippingPincode { get; set; }

    [JsonPropertyName("shipping_state")]
    public string? ShippingState { get; set; }

    [JsonPropertyName("shipping_country")]
    public string? ShippingCountry { get; set; }

    [JsonPropertyName("shipping_email")]
    public string? ShippingEmail { get; set; }

    [JsonPropertyName("shipping_phone")]
    public string? ShippingPhone { get; set; }

    [JsonPropertyName("order_items")]
    public List<ShiprocketOrderItem> OrderItems { get; set; } = new();

    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; set; } = "Prepaid"; // Prepaid or COD

    [JsonPropertyName("sub_total")]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("length")]
    public decimal Length { get; set; }

    [JsonPropertyName("breadth")]
    public decimal Breadth { get; set; }

    [JsonPropertyName("height")]
    public decimal Height { get; set; }

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }
}

public class ShiprocketOrderItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("units")]
    public int Units { get; set; }

    [JsonPropertyName("selling_price")]
    public decimal SellingPrice { get; set; }

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    [JsonPropertyName("tax")]
    public decimal Tax { get; set; }

    [JsonPropertyName("hsn")]
    public string? Hsn { get; set; }
}

public class ShiprocketCreateOrderResponse
{
    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("shipment_id")]
    public long? ShipmentId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("onboarding_completed_now")]
    public int OnboardingCompletedNow { get; set; }

    [JsonPropertyName("awb_code")]
    public string? AwbCode { get; set; }

    [JsonPropertyName("courier_company_id")]
    public int? CourierCompanyId { get; set; }

    [JsonPropertyName("courier_name")]
    public string? CourierName { get; set; }
}

#endregion

#region Shipment

public class ShiprocketGenerateAwbRequest
{
    [JsonPropertyName("shipment_id")]
    public long ShipmentId { get; set; }

    [JsonPropertyName("courier_id")]
    public int? CourierId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class ShiprocketAwbResponse
{
    [JsonPropertyName("awb_assign_status")]
    public int AwbAssignStatus { get; set; }

    [JsonPropertyName("response")]
    public ShiprocketAwbData? Response { get; set; }
}

public class ShiprocketAwbData
{
    [JsonPropertyName("data")]
    public ShiprocketAwbDetails? Data { get; set; }
}

public class ShiprocketAwbDetails
{
    [JsonPropertyName("courier_company_id")]
    public int CourierCompanyId { get; set; }

    [JsonPropertyName("awb_code")]
    public string AwbCode { get; set; } = string.Empty;

    [JsonPropertyName("cod")]
    public int Cod { get; set; }

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("shipment_id")]
    public long ShipmentId { get; set; }

    [JsonPropertyName("courier_name")]
    public string? CourierName { get; set; }

    [JsonPropertyName("assigned_date_time")]
    public ShiprocketDateTime? AssignedDateTime { get; set; }

    [JsonPropertyName("applied_weight")]
    public decimal AppliedWeight { get; set; }

    [JsonPropertyName("routing_code")]
    public string? RoutingCode { get; set; }
}

public class ShiprocketDateTime
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("timezone_type")]
    public int TimezoneType { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

#endregion

#region Courier Serviceability

public class ShiprocketServiceabilityRequest
{
    [JsonPropertyName("pickup_postcode")]
    public string PickupPostcode { get; set; } = string.Empty;

    [JsonPropertyName("delivery_postcode")]
    public string DeliveryPostcode { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("cod")]
    public int Cod { get; set; } // 0 or 1

    [JsonPropertyName("order_id")]
    public long? OrderId { get; set; }
}

public class ShiprocketServiceabilityResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("data")]
    public ShiprocketServiceabilityData? Data { get; set; }
}

public class ShiprocketServiceabilityData
{
    [JsonPropertyName("available_courier_companies")]
    public List<ShiprocketCourierCompany> AvailableCourierCompanies { get; set; } = new();

    [JsonPropertyName("child_courier_id")]
    public int? ChildCourierId { get; set; }

    [JsonPropertyName("shiprocket_recommended_courier_id")]
    public int? RecommendedCourierId { get; set; }
}

public class ShiprocketCourierCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("freight_charge")]
    public decimal FreightCharge { get; set; }

    [JsonPropertyName("cod_charges")]
    public decimal CodCharges { get; set; }

    [JsonPropertyName("coverage_charges")]
    public decimal CoverageCharges { get; set; }

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("estimated_delivery_days")]
    public string? EstimatedDeliveryDays { get; set; }

    [JsonPropertyName("etd")]
    public string? Etd { get; set; }

    [JsonPropertyName("min_weight")]
    public decimal MinWeight { get; set; }

    [JsonPropertyName("charge_weight")]
    public decimal ChargeWeight { get; set; }

    [JsonPropertyName("cod")]
    public int Cod { get; set; }

    [JsonPropertyName("mode")]
    public int Mode { get; set; } // 0 = Surface, 1 = Air

    [JsonPropertyName("is_surface")]
    public bool IsSurface { get; set; }

    [JsonPropertyName("blocked")]
    public int Blocked { get; set; }

    [JsonPropertyName("rating")]
    public decimal Rating { get; set; }

    [JsonPropertyName("suppress_date")]
    public string? SuppressDate { get; set; }
}

#endregion

#region Tracking

public class ShiprocketTrackingResponse
{
    [JsonPropertyName("tracking_data")]
    public ShiprocketTrackingData? TrackingData { get; set; }
}

public class ShiprocketTrackingData
{
    [JsonPropertyName("track_status")]
    public int TrackStatus { get; set; }

    [JsonPropertyName("shipment_status")]
    public int ShipmentStatus { get; set; }

    [JsonPropertyName("shipment_track")]
    public List<ShiprocketTrackEvent> ShipmentTrack { get; set; } = new();

    [JsonPropertyName("shipment_track_activities")]
    public List<ShiprocketTrackActivity> ShipmentTrackActivities { get; set; } = new();

    [JsonPropertyName("track_url")]
    public string? TrackUrl { get; set; }

    [JsonPropertyName("etd")]
    public string? Etd { get; set; }

    [JsonPropertyName("qc_response")]
    public ShiprocketQcResponse? QcResponse { get; set; }
}

public class ShiprocketTrackEvent
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("awb_code")]
    public string? AwbCode { get; set; }

    [JsonPropertyName("courier_company_id")]
    public int CourierCompanyId { get; set; }

    [JsonPropertyName("shipment_id")]
    public long ShipmentId { get; set; }

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("pickup_date")]
    public string? PickupDate { get; set; }

    [JsonPropertyName("delivered_date")]
    public string? DeliveredDate { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("packages")]
    public int Packages { get; set; }

    [JsonPropertyName("current_status")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("delivered_to")]
    public string? DeliveredTo { get; set; }

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    [JsonPropertyName("consignee_name")]
    public string? ConsigneeName { get; set; }

    [JsonPropertyName("origin")]
    public string? Origin { get; set; }

    [JsonPropertyName("courier_agent_details")]
    public string? CourierAgentDetails { get; set; }

    [JsonPropertyName("edd")]
    public string? Edd { get; set; }
}

public class ShiprocketTrackActivity
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("activity")]
    public string? Activity { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("sr-status")]
    public string? SrStatus { get; set; }

    [JsonPropertyName("sr-status-label")]
    public string? SrStatusLabel { get; set; }
}

public class ShiprocketQcResponse
{
    [JsonPropertyName("qc_image")]
    public string? QcImage { get; set; }

    [JsonPropertyName("qc_failed_reason")]
    public string? QcFailedReason { get; set; }
}

#endregion

#region Pickup

public class ShiprocketPickupRequest
{
    [JsonPropertyName("shipment_id")]
    public List<long> ShipmentIds { get; set; } = new();
}

public class ShiprocketPickupResponse
{
    [JsonPropertyName("pickup_status")]
    public int PickupStatus { get; set; }

    [JsonPropertyName("response")]
    public ShiprocketPickupData? Response { get; set; }
}

public class ShiprocketPickupData
{
    [JsonPropertyName("pickup_scheduled_date")]
    public string? PickupScheduledDate { get; set; }

    [JsonPropertyName("pickup_token_number")]
    public string? PickupTokenNumber { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("others")]
    public string? Others { get; set; }

    [JsonPropertyName("pickup_generated_date")]
    public ShiprocketDateTime? PickupGeneratedDate { get; set; }

    [JsonPropertyName("data")]
    public List<ShiprocketPickupShipment>? Data { get; set; }
}

public class ShiprocketPickupShipment
{
    [JsonPropertyName("shipment_id")]
    public long ShipmentId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

#endregion

#region Cancel

public class ShiprocketCancelRequest
{
    [JsonPropertyName("ids")]
    public List<long> Ids { get; set; } = new();
}

public class ShiprocketCancelResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

#endregion

#region Label

public class ShiprocketLabelResponse
{
    [JsonPropertyName("label_created")]
    public int LabelCreated { get; set; }

    [JsonPropertyName("label_url")]
    public string? LabelUrl { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("not_created")]
    public List<object>? NotCreated { get; set; }
}

#endregion

#region Common

public class ShiprocketErrorResponse
{
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, List<string>>? Errors { get; set; }
}

#endregion
