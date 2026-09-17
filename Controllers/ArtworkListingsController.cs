using ARTISTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtworkListingsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<ArtworkListing>> Get()
    {
        var listings = new[]
        {
            new ArtworkListing
            {
                Id = 1,
                Title = "Evening in Vilnius",
                Description = "An original cityscape painting.",
                Price = 120.00m,
                CreatorName = "Geronimo"
            },
            new ArtworkListing
            {
                Id = 2,
                Title = "Forest Light",
                Description = "A small original landscape painting.",
                Price = 85.00m,
                CreatorName = "Geronimo"
            }
        };

        return Ok(listings);
    }
}