﻿using Baltaio.Location.Api.Application.Data.Import.Commons;
using Baltaio.Location.Api.Domain;
using SpreadsheetLight;

namespace Baltaio.Location.Api.Infrastructure.Persistance.Repositories
{
    public class FileRepository : IFileRepository
    {
        public List<City> GetCities(Stream source)
        {
            SLDocument document;
            List<City> cities = new();

            try
            {
                document = new SLDocument(source);
                bool worksheetsExist = document.SelectWorksheet("MUNICIPIOS");
                if (!worksheetsExist)
                    return cities;
            }
            catch
            {
                return cities;
            }

            const int firstDataRow = 2;
            for (int i = firstDataRow; document.HasCellValue($"A{i}"); i++)
            {
                cities.Add(
                    new City
                    (
                        document.GetCellValueAsInt32($"A{i}"),
                        document.GetCellValueAsString($"B{i}"),
                        //int.Parse(document.GetCellValueAsString($"C{i}")),
                        null
                    )
                );
            }

            return cities;
        }

        public List<State> GetStates(Stream source)
        {
            List<State> states = new();
            SLDocument document;
            try
            {
                document = new SLDocument(source);
                bool worksheetsExist = document.SelectWorksheet("ESTADOS");
                if (!worksheetsExist)
                    return states;
            }
            catch
            {
                return states;
            }

            const int firstDataRow = 2;
            for (int i = firstDataRow; document.HasCellValue($"A{i}"); i++)
            {
                states.Add(
                    new State
                    (
                        document.GetCellValueAsInt32($"A{i}"),
                        document.GetCellValueAsString($"B{i}"),
                        document.GetCellValueAsString($"C{i}")
                    )
                );
            }

            return states;
        }
    }
}
