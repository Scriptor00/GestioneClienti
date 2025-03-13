using Bogus;
using WebAppEF.Entities;
using System;
using System.Collections.Generic;

public class OrdiniFaker
{
    private readonly Faker<Ordine> _ordineFaker;

    public OrdiniFaker()
    {
        _ordineFaker = new Faker<Ordine>()
              
            .RuleFor(o => o.IdCliente, f => f.Random.Int(1, 50))  
            .RuleFor(o => o.DataOrdine, f => f.Date.Past(1))  
            .RuleFor(o => o.Stato, f => f.PickRandom<StatoOrdine>())  
            .RuleFor(o => o.TotaleOrdine, f => f.Finance.Amount(10, 1000));  
    }

    public IEnumerable<Ordine> GenerateOrders(int numberOfOrders)
    {
        return _ordineFaker.Generate(numberOfOrders);
    }
}
