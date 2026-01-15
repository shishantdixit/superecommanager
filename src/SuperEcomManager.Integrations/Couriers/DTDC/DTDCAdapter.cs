using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.DTDC.Models;

namespace SuperEcomManager.Integrations.Couriers.DTDC;

/// <summary>
/// DTDC implementation of ICourierAdapter.
/// </summary>
public class DTDCAdapter : ICourierAdapter
{
    private readonly IDTDCClient _client;
    private readonly ILogger<DTDCAdapter> _logger;

    public CourierType CourierType => CourierType.DTDC;
    public string DisplayName => "DTDC";

    public DTDCAdapter(
        IDTDCClient client,
        ILogger<DTDCAdapter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CourierResult> ValidateCredentialsAsync(
        CourierCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = credentials.ApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                return CourierResult.Failure("API Key is required");
            }

            // Test credentials by checking a common pincode
            var response = await _client.CheckPincodeServiceabilityAsync(apiKey, "110001", cancellationToken);

            if (response == null || !response.Success)
            {
                return CourierResult.Failure(response?.Message ?? "Invalid API key");
            }

            return CourierResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC validation error");
            return CourierResult.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<List<CourierRate>>> GetRatesAsync(
        CourierCredentials credentials,
        RateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = credentials.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CourierResult<List<CourierRate>>.Failure("API Key is required");
            }

            var rateRequest = new DTDCRateRequest
            {
                OriginPincode = request.PickupPincode,
                DestinationPincode = request.DeliveryPincode,
                Weight = request.Weight,
                CodAmount = request.IsCOD ? (request.CODAmount ?? 0) : 0,
                DeclaredValue = request.DeclaredValue ?? 0,
                LoadType = "NON-DOCUMENT"
            };

            var response = await _client.GetRatesAsync(apiKey, rateRequest, cancellationToken);

            if (response == null || !response.Success || response.Data == null || response.Data.Count == 0)
            {
                // Fallback to estimated rates if API doesn't return rates
                return GetEstimatedRates(request);
            }

            var rates = response.Data.Select(r => new CourierRate
            {
                ServiceCode = r.ServiceCode ?? r.ServiceName ?? "PREMIUM",
                ServiceName = $"DTDC {r.ServiceName}",
                FreightCharge = r.FreightCharge,
                CODCharge = r.CodCharge,
                TotalCharge = r.TotalCharge,
                EstimatedDays = r.DeliveryDays,
                ExpectedDelivery = DateTime.UtcNow.AddDays(r.DeliveryDays),
                IsExpress = r.ServiceName?.ToUpper().Contains("EXPRESS") ?? false,
                IsSurface = r.ServiceName?.ToUpper().Contains("GROUND") ?? false
            }).ToList();

            return CourierResult<List<CourierRate>>.Success(rates.OrderBy(r => r.TotalCharge).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC get rates error");
            return CourierResult<List<CourierRate>>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<ShipmentResponse>> CreateShipmentAsync(
        CourierCredentials credentials,
        ShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = credentials.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CourierResult<ShipmentResponse>.Failure("API Key is required");
            }

            var customerCode = credentials.AdditionalSettings.GetValueOrDefault("customer_code", "");

            var createRequest = new DTDCCreateShipmentRequest
            {
                CustomerCode = customerCode,
                ConsignmentDetails = new List<DTDCConsignment>
                {
                    new DTDCConsignment
                    {
                        ReferenceNumber = request.OrderNumber,
                        CustomerReferenceNumber = request.OrderId,
                        ServiceName = request.IsExpress ? "EXPRESS" : (request.ServiceCode ?? "PREMIUM"),
                        LoadType = "NON-DOCUMENT",
                        NoOfPieces = 1,
                        ActualWeight = request.Weight,
                        CodAmount = request.IsCOD ? (request.CODAmount ?? request.DeclaredValue) : 0,
                        DeclaredValue = request.DeclaredValue,
                        ProductDescription = string.Join(", ", request.Items.Select(i => i.Name)),
                        IsCOD = request.IsCOD,
                        InvoiceNumber = request.OrderNumber,
                        InvoiceDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        ConsignorDetails = new DTDCPartyDetails
                        {
                            Name = request.PickupName,
                            Address1 = request.PickupAddress,
                            Pincode = request.PickupPincode,
                            City = request.PickupCity,
                            State = request.PickupState,
                            Phone = request.PickupPhone,
                            MobileNo = request.PickupPhone
                        },
                        ConsigneeDetails = new DTDCPartyDetails
                        {
                            Name = request.DeliveryName,
                            Address1 = request.DeliveryAddress,
                            Pincode = request.DeliveryPincode,
                            City = request.DeliveryCity,
                            State = request.DeliveryState,
                            Phone = request.DeliveryPhone,
                            MobileNo = request.DeliveryPhone
                        },
                        ReturnAddressDetails = new DTDCPartyDetails
                        {
                            Name = request.PickupName,
                            Address1 = request.PickupAddress,
                            Pincode = request.PickupPincode,
                            City = request.PickupCity,
                            State = request.PickupState,
                            Phone = request.PickupPhone,
                            MobileNo = request.PickupPhone
                        },
                        Dimension = (request.Length.HasValue && request.Width.HasValue && request.Height.HasValue)
                            ? new DTDCDimension
                            {
                                Length = request.Length.Value,
                                Width = request.Width.Value,
                                Height = request.Height.Value
                            }
                            : null
                    }
                }
            };

            var response = await _client.CreateShipmentAsync(apiKey, createRequest, cancellationToken);

            if (response == null || !response.Success)
            {
                return CourierResult<ShipmentResponse>.Failure(response?.Message ?? "Failed to create shipment");
            }

            var consignment = response.Data?.ConsignmentNumbers?.FirstOrDefault();
            if (consignment == null || string.IsNullOrEmpty(consignment.ConsignmentNumber))
            {
                return CourierResult<ShipmentResponse>.Failure(
                    consignment?.Message ?? "No consignment number received");
            }

            return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
            {
                AwbNumber = consignment.ConsignmentNumber,
                CourierName = "DTDC",
                ShipmentId = consignment.ConsignmentNumber,
                TrackingUrl = $"https://www.dtdc.in/tracking.asp?strCnno={consignment.ConsignmentNumber}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC create shipment error");
            return CourierResult<ShipmentResponse>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<TrackingResponse>> GetTrackingAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = credentials.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CourierResult<TrackingResponse>.Failure("API Key is required");
            }

            var response = await _client.GetTrackingAsync(apiKey, awbNumber, cancellationToken);

            if (response == null || !response.Success || response.Data == null)
            {
                return CourierResult<TrackingResponse>.Failure(
                    response?.Message ?? "Tracking information not found");
            }

            var trackingData = response.Data;

            var result = new TrackingResponse
            {
                AwbNumber = awbNumber,
                CurrentStatus = trackingData.CurrentStatus ?? "Unknown",
                CurrentLocation = trackingData.CurrentLocation,
                ExpectedDelivery = ParseDate(trackingData.ExpectedDeliveryDate),
                DeliveredAt = ParseDate(trackingData.DeliveredDate),
                DeliveredTo = trackingData.ReceivedBy,
                Events = (trackingData.TrackingHistory ?? new List<DTDCTrackingEvent>())
                    .Select(e => new TrackingEvent
                    {
                        Timestamp = ParseDateTime(e.EventDate, e.EventTime) ?? DateTime.UtcNow,
                        Status = e.Status ?? "",
                        Location = e.Location,
                        Remarks = e.Remarks
                    })
                    .ToList()
            };

            return CourierResult<TrackingResponse>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC tracking error");
            return CourierResult<TrackingResponse>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult> CancelShipmentAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = credentials.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CourierResult.Failure("API Key is required");
            }

            var customerCode = credentials.AdditionalSettings.GetValueOrDefault("customer_code", "");

            var cancelRequest = new DTDCCancelRequest
            {
                CustomerCode = customerCode,
                ConsignmentNumber = awbNumber,
                CancellationReason = "Customer Request"
            };

            var response = await _client.CancelShipmentAsync(apiKey, cancelRequest, cancellationToken);

            if (response == null || !response.Success)
            {
                return CourierResult.Failure(response?.Message ?? "Cancellation failed");
            }

            return CourierResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC cancel error");
            return CourierResult.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<byte[]>> GetLabelAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = credentials.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CourierResult<byte[]>.Failure("API Key is required");
            }

            var labelData = await _client.GetLabelAsync(apiKey, awbNumber, cancellationToken);

            if (labelData == null || labelData.Length == 0)
            {
                return CourierResult<byte[]>.Failure("Failed to generate label");
            }

            return CourierResult<byte[]>.Success(labelData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC label error");
            return CourierResult<byte[]>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<PickupResponse>> SchedulePickupAsync(
        CourierCredentials credentials,
        PickupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = credentials.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CourierResult<PickupResponse>.Failure("API Key is required");
            }

            var customerCode = credentials.AdditionalSettings.GetValueOrDefault("customer_code", "");
            var pickupName = credentials.AdditionalSettings.GetValueOrDefault("pickup_name", "");
            var pickupAddress = credentials.AdditionalSettings.GetValueOrDefault("pickup_address", "");
            var pickupPincode = credentials.AdditionalSettings.GetValueOrDefault("pickup_pincode", "");
            var pickupPhone = credentials.AdditionalSettings.GetValueOrDefault("pickup_phone", "");

            var pickupRequest = new DTDCPickupRequest
            {
                CustomerCode = customerCode,
                PickupDate = request.PickupDate.ToString("yyyy-MM-dd"),
                PickupTime = request.PickupTimeSlot ?? "10:00",
                ClosingTime = "18:00",
                ConsignmentCount = request.AwbNumbers.Count,
                TotalWeight = 0.5m * request.AwbNumbers.Count, // Estimate
                PickupAddress = new DTDCPartyDetails
                {
                    Name = pickupName,
                    Address1 = pickupAddress,
                    Pincode = pickupPincode,
                    MobileNo = pickupPhone,
                    Phone = pickupPhone
                }
            };

            var response = await _client.SchedulePickupAsync(apiKey, pickupRequest, cancellationToken);

            if (response == null || !response.Success)
            {
                return CourierResult<PickupResponse>.Failure(
                    response?.Message ?? "Pickup scheduling failed");
            }

            return CourierResult<PickupResponse>.Success(new PickupResponse
            {
                PickupId = response.Data?.PickupRequestNumber ?? response.Data?.TokenNumber,
                ScheduledDate = request.PickupDate,
                TimeSlot = response.Data?.ScheduledTime,
                ShipmentCount = request.AwbNumbers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC pickup scheduling error");
            return CourierResult<PickupResponse>.Failure(ex.Message);
        }
    }

    #region Private Helpers

    private static CourierResult<List<CourierRate>> GetEstimatedRates(RateRequest request)
    {
        var rates = new List<CourierRate>();

        // Premium (Express)
        rates.Add(new CourierRate
        {
            ServiceCode = "PREMIUM",
            ServiceName = "DTDC Premium",
            FreightCharge = CalculateEstimatedFreight(request.Weight, false),
            CODCharge = request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0,
            TotalCharge = CalculateEstimatedFreight(request.Weight, false) +
                          (request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0),
            EstimatedDays = 3,
            ExpectedDelivery = DateTime.UtcNow.AddDays(3),
            IsExpress = true,
            IsSurface = false
        });

        // Ground
        rates.Add(new CourierRate
        {
            ServiceCode = "GROUND",
            ServiceName = "DTDC Ground",
            FreightCharge = CalculateEstimatedFreight(request.Weight, true),
            CODCharge = request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0,
            TotalCharge = CalculateEstimatedFreight(request.Weight, true) +
                          (request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0),
            EstimatedDays = 6,
            ExpectedDelivery = DateTime.UtcNow.AddDays(6),
            IsExpress = false,
            IsSurface = true
        });

        return CourierResult<List<CourierRate>>.Success(rates.OrderBy(r => r.TotalCharge).ToList());
    }

    private static decimal CalculateEstimatedFreight(decimal weightKg, bool isSurface)
    {
        var baseRate = isSurface ? 40m : 55m;
        var perKgRate = isSurface ? 18m : 28m;

        var chargeableWeight = Math.Max(0.5m, Math.Ceiling(weightKg * 2) / 2);

        return baseRate + (chargeableWeight * perKgRate);
    }

    private static decimal CalculateCodCharge(decimal codAmount)
    {
        var codCharge = codAmount * 0.02m;
        return Math.Max(50m, codCharge);
    }

    private static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;

        if (DateTime.TryParse(dateString, out var date))
        {
            return date;
        }

        return null;
    }

    private static DateTime? ParseDateTime(string? date, string? time)
    {
        if (string.IsNullOrEmpty(date)) return null;

        var dateTimeStr = string.IsNullOrEmpty(time) ? date : $"{date} {time}";

        if (DateTime.TryParse(dateTimeStr, out var result))
        {
            return result;
        }

        return ParseDate(date);
    }

    #endregion
}
