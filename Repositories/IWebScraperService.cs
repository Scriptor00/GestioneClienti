using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAppEF.Repositories
{
    public interface IWebScraperService
    {
        // Metodo per generare un PDF a partire da contenuto HTML
        Task<byte[]> GeneratePdfAsync(string htmlContent);
    }
}