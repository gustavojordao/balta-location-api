﻿using DocumentFormat.OpenXml.CustomProperties;

namespace Baltaio.Location.Api.Domain;

public sealed class City
{
    public City(int code, string name, State state)
    {
        Code = code;
        Name = name;
        StateCode = state.Code;
        State = state;
    }
    private City() { }

    public int Code { get; init; }
    public string Name { get; private set; }
    public int StateCode { get; private set; }
    public State State { get; private set; }

    internal void Update(string newName, State newState)
    {
        EnsureDataIsValid();

        Name = newName;
        State = newState;
        StateCode = newState.Code;

        void EnsureDataIsValid()
        {
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentException("O nome da cidade é obrigatório.", nameof(newName));
            ArgumentNullException.ThrowIfNull(newState, nameof(State));
        }
    }
}
