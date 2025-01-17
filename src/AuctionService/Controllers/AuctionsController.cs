﻿using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuctionDto>>> GetAllAuctions(string date)
    {
        var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

        if (!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }

        return await query
            .ProjectTo<AuctionDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(
        [FromRoute] Guid id)
    {
        var auction = await _context.Auctions
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<AuctionDto>(auction));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AuctionDto>> CreateAuction(
        [FromBody] CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);

        auction.Seller = User.Identity.Name;

        _context.Auctions.Add(auction);

        var newAuction = _mapper.Map<AuctionDto>(auction);

        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest();
        }

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAuction(
        [FromRoute] Guid id,
        [FromBody] UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _context.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction == null)
        {
            return NotFound();
        }

        if (auction.Seller != User.Identity.Name)
        {
            return Forbid();
        }

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAuction(
        [FromRoute] Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);

        if (auction == null)
        {
            return NotFound();
        }

        if (auction.Seller != User.Identity.Name)
        {
            return Forbid();
        }

        _context.Auctions.Remove(auction);

        await _publishEndpoint.Publish(new AuctionDeleted
        {
            Id = auction.Id
        });

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest();
        }

        return NoContent();
    }

}
