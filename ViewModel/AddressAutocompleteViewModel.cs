namespace GestioneClienti.ViewModel
{
    public class AddressAutocompleteViewModel
    {
        public string Street { get; set; }       // ex Via/Road
        public string StreetNumber { get; set; } // ex Civico/HouseNumber
        public string City { get; set; }         // ex Citta
        public string Country { get; set; }      // ex Paese
        public string PostalCode { get; set; }   // ex Cap
        public string FormattedAddress { get; set; } // ex IndirizzoCompleto
        public string PlaceId { get; set; }           // Google Place ID

    }
}
