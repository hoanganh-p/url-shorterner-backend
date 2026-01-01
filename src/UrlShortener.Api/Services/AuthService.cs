﻿using UrlShortener.Api.Models;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services.Interfaces;

namespace UrlShortener.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repository;

        public AuthService(IAuthRepository repository)
        {
            _repository = repository;
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            return _repository.GetByEmailAsync(email);
        }

    }
}
