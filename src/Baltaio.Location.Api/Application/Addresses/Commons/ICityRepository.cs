﻿using Baltaio.Location.Api.Domain;

namespace Baltaio.Location.Api.Application.Addresses.Commons;

public interface ICityRepository
{
    public Task AddAllAsync(List<City> cities);
    public Task AddAsync(City city);
    public Task<City?> GetAsync(int ibgeCode);
    public Task<City?> SaveAsync(int IbgeCode, string NameCity, int StateCode);

    public Task<City?> GetAsync(string cityName);
    public Task<City?> GetByStateAsync(string stateAbbreviation);
}
