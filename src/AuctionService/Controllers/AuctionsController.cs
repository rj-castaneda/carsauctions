using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop.Implementation;

namespace AuctionService.Controllers
{

    [ApiController] // Checks required properties and handles errors, binding, etc.
    [Route("api/auctions")] // where to direct the http request
    public class AuctionsController : ControllerBase // Controller Base does not return view (we only care about json data)
    {
        private readonly AuctionDBContext _context;
        private readonly IMapper _mapper;

        public AuctionsController(AuctionDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet] // Action Result allows to send back http responses
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions() 
        {
            var auctions = await _context.Auctions
                .Include(x => x.Item) // eagerly load related property Item into memory
                .OrderBy(x => x.Item.Make) 
                .ToListAsync();
            
            // map to a list of auction dtos, and get that from the auctions
            return _mapper.Map<List<AuctionDto>>(auctions);
        }

        [HttpGet("{id}")] // api/auctions/1
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            var auction = await _context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return NotFound();

            return _mapper.Map<AuctionDto>(auction);
        }

        [HttpPost]   
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        {
            var auction = _mapper.Map<Auction>(auctionDto);
            // TODO: Add current user a s seller
            auction.Seller = "test";

            _context.Auctions.Add(auction);

            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Could not save changes to the Database");

            return  CreatedAtAction(nameof(GetAuctionById), new {auction.Id}, _mapper.Map<AuctionDto>(auction));
        }
        
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);

            if(auction == null) return NotFound();
            // TODO: Seller matches the user who created the auction

            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

            var result = await _context.SaveChangesAsync() > 0;
            
            if (!result) return BadRequest("Could not update auction");

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var auction = await _context.Auctions.FindAsync(id);
            if (auction == null) return NotFound();
            // TODO: check seller == username

            _context.Auctions.Remove(auction);

            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Could not delete auction");

            return Ok();
        }

    }
}