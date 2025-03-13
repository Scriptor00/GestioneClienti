using Bogus;
using WebAppEF.Entities;
using System;
using System.Collections.Generic;

public class DataSeeder
{
    public static List<Cliente> GeneraClienti(int count)
    {
        var faker = new Faker("it"); 

        return new Faker<Cliente>()
            .RuleFor(c => c.IdCliente, f => 0) 
            .RuleFor(c => c.Nome, f => f.Name.FirstName()) 
            .RuleFor(c => c.Cognome, f => f.Name.LastName()) 
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Nome, c.Cognome)) 
            .RuleFor(c => c.DataIscrizione, f => f.Date.Past(5, DateTime.Now)) 
            .RuleFor(c => c.Attivo, f => f.Random.Bool(0.8f)) 
            .Generate(count);
    }
}
