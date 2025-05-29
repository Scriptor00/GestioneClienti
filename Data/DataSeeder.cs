using Bogus;
using WebAppEF.Entities;

public class DataSeeder
{
    public static List<Cliente> GeneraClienti(int count)
    {
        var faker = new Faker<Cliente>("it_IT") // Usa il localizzatore italiano
            .RuleFor(c => c.IdCliente, f => 0)
            .RuleFor(c => c.Nome, f => f.Name.FirstName())
            .RuleFor(c => c.Cognome, f => f.Name.LastName())
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Nome, c.Cognome))
            .RuleFor(c => c.DataIscrizione, f => f.Date.Past(5, DateTime.Now))
            .RuleFor(c => c.Attivo, f => f.Random.Bool(0.8f))
            .RuleFor(c => c.Indirizzo, f => f.Address.StreetAddress())
            .RuleFor(c => c.Civico, f => f.Address.BuildingNumber())
            .RuleFor(c => c.Citta, f => f.Address.City())
            .RuleFor(c => c.Paese, f => f.Address.Country())
            .RuleFor(c => c.Cap, f => f.Address.ZipCode());

        return faker.Generate(count);
    }

    public static List<Cliente> AggiornaClientiConIndirizzo(List<Cliente> clientiEsistenti)
    {
        var faker = new Faker<Cliente>("it_IT") 
            .RuleFor(c => c.Indirizzo, f => f.Address.StreetAddress())
            .RuleFor(c => c.Civico, f => f.Address.BuildingNumber())
            .RuleFor(c => c.Citta, f => f.Address.City())
            .RuleFor(c => c.Paese, f => f.Address.Country())
            .RuleFor(c => c.Cap, f => f.Address.ZipCode());

        foreach (var cliente in clientiEsistenti)
        {
            var nuovoIndirizzo = faker.Generate();
            cliente.Indirizzo = nuovoIndirizzo.Indirizzo;
            cliente.Civico = nuovoIndirizzo.Civico;
            cliente.Citta = nuovoIndirizzo.Citta;
            cliente.Paese = nuovoIndirizzo.Paese;
            cliente.Cap = nuovoIndirizzo.Cap;
        }

        return clientiEsistenti;
    }
}