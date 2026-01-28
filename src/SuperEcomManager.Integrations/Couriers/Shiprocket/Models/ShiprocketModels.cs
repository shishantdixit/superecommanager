using System.Text.Json;
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
    public int ShippingIsBilling { get; set; } = 1; // 1 = true, 0 = false

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
    [JsonConverter(typeof(DecimalToStringConverter))]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("length")]
    [JsonConverter(typeof(DecimalToStringConverter))]
    public decimal Length { get; set; }

    [JsonPropertyName("breadth")]
    [JsonConverter(typeof(DecimalToStringConverter))]
    public decimal Breadth { get; set; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(DecimalToStringConverter))]
    public decimal Height { get; set; }

    [JsonPropertyName("weight")]
    [JsonConverter(typeof(DecimalToStringConverter))]
    public decimal Weight { get; set; }
}

public class ShiprocketOrderItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("units")]
    [JsonConverter(typeof(NumberToStringConverter))]
    public int Units { get; set; }

    [JsonPropertyName("selling_price")]
    [JsonConverter(typeof(DecimalToStringConverter))]
    public decimal SellingPrice { get; set; }

    [JsonPropertyName("discount")]
    [JsonConverter(typeof(DecimalToStringConverter))]
    public decimal Discount { get; set; }

    [JsonPropertyName("tax")]
    [JsonConverter(typeof(DecimalToStringConverter))]
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
    [JsonConverter(typeof(FlexibleNullableIntConverter))]
    public int? CourierCompanyId { get; set; }

    [JsonPropertyName("courier_name")]
    public string? CourierName { get; set; }

    // Error fields (present when order creation fails)
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, List<string>>? Errors { get; set; }
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

#region Orders Management

public class ShiprocketOrdersResponse
{
    [JsonPropertyName("data")]
    public ShiprocketOrdersList? Data { get; set; }
}

public class ShiprocketOrdersList
{
    [JsonPropertyName("data")]
    public List<ShiprocketOrderInfo>? Orders { get; set; }

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class ShiprocketOrderInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("channel_order_id")]
    public string? ChannelOrderId { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("sub_total")]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}

public class ShiprocketOrderDetailResponse
{
    [JsonPropertyName("data")]
    public ShiprocketOrderDetail? Data { get; set; }
}

public class ShiprocketOrderDetail
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("channel_order_id")]
    public string? ChannelOrderId { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customer_email")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("customer_phone")]
    public string? CustomerPhone { get; set; }

    [JsonPropertyName("billing_address")]
    public string? BillingAddress { get; set; }

    [JsonPropertyName("billing_city")]
    public string? BillingCity { get; set; }

    [JsonPropertyName("billing_state")]
    public string? BillingState { get; set; }

    [JsonPropertyName("billing_pincode")]
    public string? BillingPincode { get; set; }

    [JsonPropertyName("shipping_address")]
    public string? ShippingAddress { get; set; }

    [JsonPropertyName("shipping_city")]
    public string? ShippingCity { get; set; }

    [JsonPropertyName("shipping_state")]
    public string? ShippingState { get; set; }

    [JsonPropertyName("shipping_pincode")]
    public string? ShippingPincode { get; set; }

    [JsonPropertyName("order_items")]
    public List<ShiprocketOrderItem>? OrderItems { get; set; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("sub_total")]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("shipments")]
    public List<ShiprocketShipmentInfo>? Shipments { get; set; }
}

public class ShiprocketUpdateOrderRequest
{
    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("shipping_charges")]
    public decimal? ShippingCharges { get; set; }

    [JsonPropertyName("giftwrap_charges")]
    public decimal? GiftwrapCharges { get; set; }

    [JsonPropertyName("transaction_charges")]
    public decimal? TransactionCharges { get; set; }

    [JsonPropertyName("total_discount")]
    public decimal? TotalDiscount { get; set; }
}

public class ShiprocketUpdateOrderResponse
{
    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

#endregion

#region Shipments

public class ShiprocketShipmentsResponse
{
    [JsonPropertyName("data")]
    public ShiprocketShipmentsList? Data { get; set; }
}

public class ShiprocketShipmentsList
{
    [JsonPropertyName("data")]
    public List<ShiprocketShipmentInfo>? Shipments { get; set; }

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class ShiprocketShipmentInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("awb_code")]
    public string? AwbCode { get; set; }

    [JsonPropertyName("courier_name")]
    public string? CourierName { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("shipment_status")]
    public string? ShipmentStatus { get; set; }

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("channel_order_id")]
    public string? ChannelOrderId { get; set; }

    [JsonPropertyName("pickup_scheduled_date")]
    public string? PickupScheduledDate { get; set; }

    [JsonPropertyName("delivered_date")]
    public string? DeliveredDate { get; set; }
}

public class ShiprocketShipmentDetailResponse
{
    [JsonPropertyName("data")]
    public ShiprocketShipmentDetail? Data { get; set; }
}

public class ShiprocketShipmentDetail
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("awb_code")]
    public string? AwbCode { get; set; }

    [JsonPropertyName("courier_company_id")]
    [JsonConverter(typeof(FlexibleNullableIntConverter))]
    public int? CourierCompanyId { get; set; }

    [JsonPropertyName("courier_name")]
    public string? CourierName { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("current_status")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("order")]
    public ShiprocketOrderInfo? Order { get; set; }
}

#endregion

#region Returns

public class ShiprocketCreateReturnRequest
{
    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("channel_id")]
    public int ChannelId { get; set; }

    [JsonPropertyName("pickup_customer_name")]
    public string? PickupCustomerName { get; set; }

    [JsonPropertyName("pickup_address")]
    public string? PickupAddress { get; set; }

    [JsonPropertyName("pickup_city")]
    public string? PickupCity { get; set; }

    [JsonPropertyName("pickup_state")]
    public string? PickupState { get; set; }

    [JsonPropertyName("pickup_pincode")]
    public string? PickupPincode { get; set; }

    [JsonPropertyName("pickup_phone")]
    public string? PickupPhone { get; set; }

    [JsonPropertyName("shipping_customer_name")]
    public string? ShippingCustomerName { get; set; }

    [JsonPropertyName("shipping_address")]
    public string? ShippingAddress { get; set; }

    [JsonPropertyName("shipping_city")]
    public string? ShippingCity { get; set; }

    [JsonPropertyName("shipping_state")]
    public string? ShippingState { get; set; }

    [JsonPropertyName("shipping_pincode")]
    public string? ShippingPincode { get; set; }

    [JsonPropertyName("shipping_phone")]
    public string? ShippingPhone { get; set; }

    [JsonPropertyName("order_items")]
    public List<ShiprocketOrderItem>? OrderItems { get; set; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("total_discount")]
    public decimal TotalDiscount { get; set; }

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

public class ShiprocketReturnResponse
{
    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("shipment_id")]
    public long? ShipmentId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ShiprocketReturnsListResponse
{
    [JsonPropertyName("data")]
    public List<ShiprocketReturnInfo>? Data { get; set; }
}

public class ShiprocketReturnInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("channel_order_id")]
    public string? ChannelOrderId { get; set; }

    [JsonPropertyName("shipment_id")]
    public long? ShipmentId { get; set; }

    [JsonPropertyName("awb_code")]
    public string? AwbCode { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

#endregion

#region Manifest

public class ShiprocketManifestRequest
{
    [JsonPropertyName("shipment_id")]
    public List<long> ShipmentIds { get; set; } = new();
}

public class ShiprocketManifestResponse
{
    [JsonPropertyName("manifest_url")]
    public string? ManifestUrl { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ShiprocketPrintManifestRequest
{
    [JsonPropertyName("order_ids")]
    public List<long> OrderIds { get; set; } = new();
}

public class ShiprocketPrintManifestResponse
{
    [JsonPropertyName("manifest_url")]
    public string? ManifestUrl { get; set; }

    [JsonPropertyName("is_generated")]
    public int IsGenerated { get; set; }
}

#endregion

#region Pickup

public class ShiprocketCancelPickupRequest
{
    [JsonPropertyName("shipment_id")]
    public List<long> ShipmentIds { get; set; } = new();
}

public class ShiprocketCancelPickupResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

#endregion

#region Pickup Locations

public class ShiprocketPickupLocationsResponse
{
    [JsonPropertyName("data")]
    public ShiprocketPickupLocationsData? Data { get; set; }
}

public class ShiprocketPickupLocationsData
{
    [JsonPropertyName("shipping_address")]
    public List<ShiprocketPickupLocationInfo>? ShippingAddress { get; set; }
}

public class ShiprocketPickupLocationInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("pickup_location")]
    public string? PickupLocation { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("address_2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("pin_code")]
    public string? PinCode { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

#endregion

#region Wallet

public class ShiprocketWalletResponse
{
    [JsonPropertyName("data")]
    public ShiprocketWalletData? Data { get; set; }
}

public class ShiprocketWalletData
{
    [JsonPropertyName("balance_amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Balance { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }
}

#endregion

#region Channels

public class ShiprocketChannelsResponse
{
    [JsonPropertyName("data")]
    public List<ShiprocketChannelInfo>? Data { get; set; }
}

public class ShiprocketChannelInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class ShiprocketCreateChannelRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class ShiprocketChannelResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

#endregion

#region Inventory

public class ShiprocketProductsResponse
{
    [JsonPropertyName("data")]
    public ShiprocketProductsList? Data { get; set; }
}

public class ShiprocketProductsList
{
    [JsonPropertyName("data")]
    public List<ShiprocketProductInfo>? Products { get; set; }

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class ShiprocketProductInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("selling_price")]
    public decimal SellingPrice { get; set; }

    [JsonPropertyName("inventory")]
    public int Inventory { get; set; }
}

public class ShiprocketAddProductRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("selling_price")]
    public decimal SellingPrice { get; set; }

    [JsonPropertyName("length")]
    public decimal? Length { get; set; }

    [JsonPropertyName("breadth")]
    public decimal? Breadth { get; set; }

    [JsonPropertyName("height")]
    public decimal? Height { get; set; }

    [JsonPropertyName("weight")]
    public decimal? Weight { get; set; }
}

public class ShiprocketAddProductResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ShiprocketUpdateInventoryRequest
{
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("inventory")]
    public int Inventory { get; set; }
}

public class ShiprocketUpdateInventoryResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

#endregion

#region Courier Partners

public class ShiprocketCourierPartnersResponse
{
    [JsonPropertyName("data")]
    public ShiprocketCourierPartnersList? Data { get; set; }
}

public class ShiprocketCourierPartnersList
{
    [JsonPropertyName("shipping_partners")]
    public List<ShiprocketCourierPartner>? Partners { get; set; }
}

public class ShiprocketCourierPartner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("first_activated_on")]
    public string? FirstActivatedOn { get; set; }

    [JsonPropertyName("is_active")]
    public int IsActive { get; set; }
}

#endregion

#region NDR

public class ShiprocketNdrActionRequest
{
    [JsonPropertyName("awb_code")]
    public string? AwbCode { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; } // re-attempt, rto

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class ShiprocketNdrResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class ShiprocketNdrListResponse
{
    [JsonPropertyName("data")]
    public List<ShiprocketNdrInfo>? Data { get; set; }
}

public class ShiprocketNdrInfo
{
    [JsonPropertyName("awb")]
    public string? Awb { get; set; }

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("ndr_status")]
    public string? NdrStatus { get; set; }

    [JsonPropertyName("ndr_status_description")]
    public string? NdrStatusDescription { get; set; }

    [JsonPropertyName("buyer_name")]
    public string? BuyerName { get; set; }

    [JsonPropertyName("buyer_phone")]
    public string? BuyerPhone { get; set; }
}

#endregion

#region Weight Reconciliation

public class ShiprocketWeightDisputesResponse
{
    [JsonPropertyName("data")]
    public ShiprocketWeightDisputesList? Data { get; set; }
}

public class ShiprocketWeightDisputesList
{
    [JsonPropertyName("data")]
    public List<ShiprocketWeightDispute>? Disputes { get; set; }

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }
}

public class ShiprocketWeightDispute
{
    [JsonPropertyName("awb_code")]
    public string? AwbCode { get; set; }

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("charged_weight")]
    public decimal ChargedWeight { get; set; }

    [JsonPropertyName("applied_weight")]
    public decimal AppliedWeight { get; set; }

    [JsonPropertyName("dispute_status")]
    public string? DisputeStatus { get; set; }
}

#endregion

#region Webhooks

public class ShiprocketCreateWebhookRequest
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("carrier_id")]
    public int? CarrierId { get; set; }

    [JsonPropertyName("events")]
    public List<string>? Events { get; set; }
}

public class ShiprocketUpdateWebhookRequest
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("events")]
    public List<string>? Events { get; set; }
}

public class ShiprocketWebhookResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ShiprocketDeleteWebhookResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
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

/// <summary>
/// Custom JSON converter that handles flexible int? deserialization.
/// Can convert from: int, long, string (numeric), null, empty string.
/// </summary>
public class FlexibleNullableIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt32();

            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;

                if (int.TryParse(stringValue, out var intValue))
                    return intValue;

                return null;

            case JsonTokenType.Null:
                return null;

            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Converts integer to string for Shiprocket API (writes as string, reads from both).
/// </summary>
public class NumberToStringConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (int.TryParse(reader.GetString(), out var value))
                return value;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Converts decimal to string for Shiprocket API (writes as string, reads from both).
/// </summary>
public class DecimalToStringConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (decimal.TryParse(reader.GetString(), out var value))
                return value;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("0.##"));
    }
}

#endregion
