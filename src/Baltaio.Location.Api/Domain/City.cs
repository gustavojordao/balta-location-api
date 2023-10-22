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

    public int Code { get; set; }
    public string Name { get; set; }
    public int StateCode { get; set; }
    public State State { get; set; }
}
