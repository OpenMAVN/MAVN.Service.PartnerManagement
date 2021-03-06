﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Geohash;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using MAVN.Service.Credentials.Client;
using MAVN.Service.Credentials.Client.Models.Requests;
using MAVN.Service.Credentials.Client.Models.Responses;
using MAVN.Service.CustomerProfile.Client;
using MAVN.Service.PartnerManagement.Contract;
using MAVN.Service.PartnerManagement.Domain.Exceptions;
using MAVN.Service.PartnerManagement.Domain.Models;
using MAVN.Service.PartnerManagement.Domain.Models.Dto;
using MAVN.Service.PartnerManagement.Domain.Repositories;
using MAVN.Service.PartnerManagement.Domain.Services;
using MAVN.Service.PartnerManagement.DomainServices.Helpers;
using MAVN.Service.Referral.Client;
using MAVN.Service.Referral.Client.Enums;
using MAVN.Service.Referral.Client.Models.Requests;
using MoreLinq;

namespace MAVN.Service.PartnerManagement.DomainServices
{
    public class PartnerService : IPartnerService
    {
        private const double MaxRadiusInKm = 128;

        private readonly IPartnerRepository _partnerRepository;
        private readonly ILocationService _locationService;
        private readonly ICredentialsClient _credentialsClient;
        private readonly ICustomerProfileClient _customerProfileClient;
        private readonly IReferralClient _referralClient;
        private readonly ILocationRepository _locationRepository;
        private readonly IRabbitPublisher<PartnerCreatedEvent> _partnerCreatedPublisher;
        private readonly IMapper _mapper;
        private readonly Geohasher _geohasher = new Geohasher();
        private readonly ILog _log;

        public PartnerService(
            IPartnerRepository partnerRepository,
            ILocationService locationService,
            ICredentialsClient credentialsClient,
            ICustomerProfileClient customerProfileClient,
            IReferralClient referralClient,
            ILocationRepository locationRepository,
            IRabbitPublisher<PartnerCreatedEvent> partnerCreatedPublisher,
            IMapper mapper,
            ILogFactory logFactory)
        {
            _partnerRepository = partnerRepository;
            _locationService = locationService;
            _credentialsClient = credentialsClient;
            _customerProfileClient = customerProfileClient;
            _referralClient = referralClient;
            _locationRepository = locationRepository;
            _partnerCreatedPublisher = partnerCreatedPublisher;
            _mapper = mapper;
            _log = logFactory.CreateLog(this);
        }

        public async Task<Guid> CreateAsync(Partner partner)
        {
            partner.Id = Guid.NewGuid();

            await _locationService.CreateLocationsContactPersonForPartnerAsync(partner);

            if (partner.UseGlobalCurrencyRate)
            {
                partner.AmountInTokens = null;
                partner.AmountInCurrency = null;
            }

            CredentialsCreateResponse credential;

            try
            {
                credential = await _credentialsClient.Partners.CreateAsync(new PartnerCredentialsCreateRequest
                {
                    PartnerId = partner.Id.ToString(),
                    ClientId = partner.ClientId,
                    ClientSecret = partner.ClientSecret
                });
            }
            catch (ClientApiException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.BadRequest)
                {
                    throw new PartnerRegistrationFailedException(e.ErrorResponse.ErrorMessage);
                }

                throw;
            }

            if (credential.Error == CredentialsError.LoginAlreadyExists)
            {
                var exception = new ClientAlreadyExistException($"Client with id '{partner.ClientId}' already exist.");
                _log.Warning(exception.Message, context: partner);
                throw exception;
            }

            if (credential.Error != CredentialsError.None)
            {
                var exception = new PartnerRegistrationFailedException("Register user failed.");
                _log.Warning(exception.Message, context: partner);
                throw exception;
            }

            var createdPartner = await _partnerRepository.CreateAsync(partner);

            var referralResponse = await _referralClient.ReferralApi.PostAsync(new ReferralCreateRequest { CustomerId = createdPartner.Id.ToString() });

            if (referralResponse.ErrorCode != ReferralErrorCodes.None)
                _log.Warning("Referral code creation failed for partner", context: new { referralResponse.ErrorCode, PartnerId = createdPartner.Id });

            await _partnerCreatedPublisher.PublishAsync(new PartnerCreatedEvent
            {
                CreatedBy = createdPartner.CreatedBy,
                PartnerId = createdPartner.Id,
                Timestamp = DateTime.UtcNow
            });

            return createdPartner.Id;
        }

        public async Task UpdateAsync(Partner partner)
        {
            var existingPartner = await _partnerRepository.GetByIdAsync(partner.Id);

            if (existingPartner == null)
            {
                throw new PartnerNotFoundFailedException($"Partner with id '{partner.Id}' not found.");
            }

            if (string.IsNullOrEmpty(partner.ClientId))
            {
                partner.ClientId = existingPartner.ClientId;
            }

            if (!string.IsNullOrEmpty(partner.ClientSecret))
            {
                CredentialsUpdateResponse credentialUpdate;

                try
                {
                    credentialUpdate = await _credentialsClient.Partners.UpdateAsync(new PartnerCredentialsUpdateRequest
                    {
                        PartnerId = partner.Id.ToString(),
                        ClientId = partner.ClientId,
                        ClientSecret = partner.ClientSecret
                    });
                }
                catch (ClientApiException e)
                {
                    if (e.HttpStatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new PartnerRegistrationFailedException(e.ErrorResponse.ErrorMessage);
                    }

                    throw;
                }

                if (credentialUpdate.Error == CredentialsError.LoginAlreadyExists)
                {
                    var exception = new ClientAlreadyExistException($"Client with id '{partner.ClientId}' already exist.");
                    _log.Warning(exception.Message, context: partner);
                    throw exception;
                }

                if (credentialUpdate.Error != CredentialsError.None)
                {
                    var exception = new PartnerRegistrationUpdateFailedException($"Updating the credentials for partner {partner.Id} failed.");
                    _log.Warning(exception.Message, context: partner);
                    throw exception;
                }
            }

            if (partner.UseGlobalCurrencyRate)
            {
                partner.AmountInTokens = null;
                partner.AmountInCurrency = null;
            }

            partner.CreatedAt = existingPartner.CreatedAt;
            partner.CreatedBy = existingPartner.CreatedBy;

            partner.Locations = await _locationService.UpdateRangeAsync(partner, partner.Locations, existingPartner.Locations);

            await _partnerRepository.UpdateAsync(partner);
        }

        public async Task DeleteAsync(Guid partnerId)
        {
            var partner = await _partnerRepository.GetByIdAsync(partnerId);

            if (partner == null)
            {
                return;
            }

            var asyncActions = new List<Task>();
            partner.Locations?.ForEach(l => { asyncActions.Add(DeleteContactPerson(l)); });

            await Task.WhenAll(asyncActions);

            await _partnerRepository.DeleteAsync(partnerId);
        }

        public async Task<(IReadOnlyCollection<Partner> partners, int totalSize)> GetAsync(PartnerListRequestDto model)
        {
            return await _partnerRepository.GetAsync(model);
        }

        public async Task<Partner> GetByIdAsync(Guid partnerId)
        {
            var partner = await _partnerRepository.GetByIdAsync(partnerId);

            return await EnrichPartner(partner);
        }

        public async Task<Partner> GetByClientIdAsync(string clientId)
        {
            var partner = await _partnerRepository.GetByClientIdAsync(clientId);

            return await EnrichPartner(partner);
        }

        public Task<IReadOnlyCollection<Partner>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var partners = _partnerRepository.GetByIdsAsync(ids);

            return partners;
        }

        public async Task<Partner> GetByLocationIdAsync(Guid locationId)
        {
            var partner = await _partnerRepository.GetByLocationIdAsync(locationId);

            return await EnrichPartner(partner);
        }

        public async Task<Guid[]> GetNearPartnerIdsAsync(double? radiusInKm, double? longitude, double? latitude, string iso3Code)
        {
            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                throw new ArgumentException("Invalid argument value for get near partners request");

            var searchByCoordinates = latitude.HasValue && longitude.HasValue;

            var locations = searchByCoordinates
                ? await GetLocationsByCoordinates(latitude.Value, longitude.Value, radiusInKm, iso3Code)
                : await _locationRepository.GetLocationsByFilterAsync(geohash: null, iso3Code);

            var result = locations
                .Select(l => l.PartnerId)
                .Distinct()
                .ToArray();

            return result;
        }

        private async Task<IEnumerable<Location>> GetLocationsByCoordinates(double latitude, double longitude, double? radiusInKm, string iso3Code)
        {
            string geohash = null;
            var locationsCount = 0;
            IEnumerable<Location> locations = null;
            const int locationsLimit = 20;
            var shouldSearchIteratively = !radiusInKm.HasValue;
            radiusInKm = radiusInKm ?? 8;

            //Try to get locations in radius if there are not locations in this radius,
            //increase it and try again until you find location or the radius exceeds the maximum allowed
            do
            {
                var geohashLevel = DistanceHelper.GetGeohashLevelByRadius(radiusInKm.Value);
                geohash = _geohasher.Encode(latitude, longitude, precision: geohashLevel);

                locations = await _locationRepository.GetLocationsByFilterAsync(geohash, iso3Code);

                if (locations.Any())
                {
                    var locationDistances = new Dictionary<Guid, double>();
                    foreach (var location in locations)
                    {
                        var distance = DistanceHelper.GetDistanceInKmBetweenTwoPoints(
                                           latitude, longitude, location.Latitude.Value,
                                           location.Longitude.Value);

                        if (distance <= radiusInKm.Value)
                            locationDistances.Add(location.Id, distance);
                    }

                    //Additional precise filtering
                    locations = locations
                        .Where(l => locationDistances.ContainsKey(l.Id))
                        .OrderBy(l => locationDistances[l.Id]);
                }

                locationsCount = locations.Count();
                radiusInKm *= 2;

            } while (shouldSearchIteratively && radiusInKm.Value <= MaxRadiusInKm && locationsCount < locationsLimit);

            return locations;
        }

        private async Task<Partner> EnrichPartner(Partner partner)
        {
            if (partner == null)
                return null;

            var asyncActions = new List<Task>();
            partner.Locations?.ForEach(l => { asyncActions.Add(GetContactPerson(l)); });

            await Task.WhenAll(asyncActions);

            string referralCode = null;
            var referralCodeResponse = await _referralClient.ReferralApi.GetAsync(partner.Id.ToString());

            if (referralCodeResponse.ErrorCode != ReferralErrorCodes.None)
            {
                var createReferralCodeResponse =
                    await _referralClient.ReferralApi.PostAsync(
                        new ReferralCreateRequest {CustomerId = partner.Id.ToString()});

                if (createReferralCodeResponse.ErrorCode != ReferralErrorCodes.None)
                    _log.Warning("Referral code creation failed for partner",
                        context: new {createReferralCodeResponse.ErrorCode, PartnerId = partner.Id});
                else
                    referralCode = createReferralCodeResponse.ReferralCode;
            }
            else
            {
                referralCode = referralCodeResponse.ReferralCode;
            }

            partner.ReferralCode = referralCode;

            return partner;
        }

        private async Task GetContactPerson(Location location)
        {
            var partnerContact = await _customerProfileClient.PartnerContact.GetByLocationIdAsync(location.Id.ToString());
            location.ContactPerson = _mapper.Map<ContactPerson>(partnerContact.PartnerContact);
        }

        private async Task DeleteContactPerson(Location location)
        {
            await _customerProfileClient.PartnerContact.DeleteIfExistAsync(location.Id.ToString());
            location.ContactPerson = null;
        }
    }
}
