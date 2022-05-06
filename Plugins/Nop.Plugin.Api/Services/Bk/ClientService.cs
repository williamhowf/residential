﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Api.Services
{
    using IdentityModel;
    using IdentityServer4;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using IdentityServer4.Models;
    using Microsoft.EntityFrameworkCore;
    using Nop.Plugin.Api.MappingExtensions;
    using Nop.Plugin.Api.Models;
    using Client = IdentityServer4.EntityFramework.Entities.Client;

    public class ClientService : IClientService
    {
        private readonly IConfigurationDbContext _configurationDbContext;

        public ClientService(IConfigurationDbContext configurationDbContext)
        {
            _configurationDbContext = configurationDbContext;
        }
        
        public IList<ClientApiModel> GetAllClients()
        {
            IQueryable<Client> clientsQuery = _configurationDbContext.Clients
                .Include(x => x.ClientSecrets)
                .Include(x => x.RedirectUris);

            IList<Client> clients = clientsQuery.ToList();

            IList<ClientApiModel> clientApiModels = clients.Select(client => client.ToApiModel()).ToList();

            return clientApiModels;
        }
        
        public int InsertClient(ClientApiModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var client = new Client()
            {
                ClientId = model.ClientId,
                Enabled = model.Enabled,
                ClientName = model.ClientName,
                // Needed to be able to obtain refresh token.
                AllowOfflineAccess = true,
                AccessTokenLifetime = model.AccessTokenLifetime,
                AbsoluteRefreshTokenLifetime = model.RefreshTokenLifetime,
				RequireClientSecret = false		// added by WKK 6Aug2018 for Jira MSP-21 modify API token
            };

            AddOrUpdateClientSecret(client, model.ClientSecret);
            AddOrUpdateClientRedirectUrl(client, model.RedirectUrl);
            
            client.AllowedGrantTypes = new List<ClientGrantType>()
            {
                new ClientGrantType()
                {
                    Client = client,
                    GrantType = OidcConstants.GrantTypes.AuthorizationCode
                },
                new ClientGrantType()
                {
                    Client = client,
                    GrantType = OidcConstants.GrantTypes.RefreshToken
                },
                new ClientGrantType()
                {
                    Client = client,
                    GrantType = OidcConstants.GrantTypes.JwtBearer
                }
            };

            client.AllowedScopes = new List<ClientScope>()
            {
                new ClientScope()
                {
                    Client = client,
                    Scope = "nop_api"
                }
            };

            client.Claims = new List<ClientClaim>()
            {
                new ClientClaim()
                {
                    Client = client,
                    Type = JwtClaimTypes.Subject,
                    Value = client.ClientId
                },
                new ClientClaim()
                {
                    Client = client,
                    Type = JwtClaimTypes.Name,
                    Value = client.ClientName
                }

            };

            _configurationDbContext.Clients.Add(client);
            _configurationDbContext.SaveChanges();

            return client.Id;
        }

        public void UpdateClient(ClientApiModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            
            Client currentClient = _configurationDbContext.Clients
                .Include(client => client.ClientSecrets)
                .Include(client => client.RedirectUris)
                .FirstOrDefault(client => client.ClientId == model.ClientId);

            if (currentClient == null)
            {
                throw new ArgumentNullException(nameof(currentClient));
            }

            AddOrUpdateClientSecret(currentClient, model.ClientSecret);
            AddOrUpdateClientRedirectUrl(currentClient, model.RedirectUrl);

            currentClient.ClientId = model.ClientId;
            currentClient.ClientName = model.ClientName;
            currentClient.Enabled = model.Enabled;
            currentClient.AccessTokenLifetime = model.AccessTokenLifetime;
            currentClient.AbsoluteRefreshTokenLifetime = model.RefreshTokenLifetime;

            _configurationDbContext.Clients.Update(currentClient);
            _configurationDbContext.SaveChanges();
        }

        public ClientApiModel FindClientByIdAsync(int id)
        {
            Client currentClient = _configurationDbContext.Clients
                .Include(client => client.ClientSecrets)
                .Include(client => client.RedirectUris)
                .FirstOrDefault(client => client.Id == id);

            return currentClient?.ToApiModel();
        }

        public ClientApiModel FindClientByClientId(string clientId)
        {
            Client currentClient = _configurationDbContext.Clients
                .Include(client => client.ClientSecrets)
                .Include(client => client.RedirectUris)
                .FirstOrDefault(client => client.ClientId == clientId);

            return currentClient?.ToApiModel();
        }

        public void DeleteClient(int id)
        {
            Client client = _configurationDbContext.Clients
                .Include(entity => entity.ClientSecrets)
                .Include(entity => entity.RedirectUris)
                .FirstOrDefault(x => x.Id == id);

            if (client != null)
            {
                _configurationDbContext.Clients.Remove(client);
                _configurationDbContext.SaveChanges();
            }
        }
        
        private void AddOrUpdateClientRedirectUrl(Client currentClient, string modelRedirectUrl)
        {
            // Ensure the client redirect url collection is not null
            if (currentClient.RedirectUris == null)
            {
                currentClient.RedirectUris = new List<ClientRedirectUri>();
            }

            // Currently, the api works with only one client redirect uri.
            ClientRedirectUri currentClientRedirectUri = currentClient.RedirectUris.FirstOrDefault();

            // Add new redirectUri
            if ((currentClientRedirectUri != null && currentClientRedirectUri.RedirectUri != modelRedirectUrl) ||
                currentClientRedirectUri == null)
            {
                // Remove all redirect uris as we may have only one.
                currentClient.RedirectUris.Clear();

                currentClient.RedirectUris.Add(new ClientRedirectUri()
                {
                    Client = currentClient,
                    RedirectUri = modelRedirectUrl
                });
            }
        }

        private void AddOrUpdateClientSecret(Client currentClient, string modelClientSecretDescription)
        {
            // Ensure the client secrets collection is not null
            if (currentClient.ClientSecrets == null)
            {
                currentClient.ClientSecrets = new List<ClientSecret>();
            }

            // Currently, the api works with only one client secret.
            ClientSecret currentClientSecret = currentClient.ClientSecrets.FirstOrDefault();

            // Add new secret
            if ((currentClientSecret != null && currentClientSecret.Description != modelClientSecretDescription) ||
                currentClientSecret == null)
            {
                // Remove all secrets as we may have only one valid.
                currentClient.ClientSecrets.Clear();

                currentClient.ClientSecrets.Add(new ClientSecret()
                {
                    Client = currentClient,
                    Value = modelClientSecretDescription.Sha256(),
                    Type = IdentityServerConstants.ParsedSecretTypes.SharedSecret,
                    Description = modelClientSecretDescription
                });
            }
        }
    }
}