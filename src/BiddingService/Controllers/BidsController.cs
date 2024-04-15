﻿using AutoMapper;
using BiddingService.DTOs;
using BiddingService.Models;
using BiddingService.Services;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace BiddingService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BidsController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly IPublishEndpoint _publishEndpoint;
	private readonly GrpAuctionClient _grpcClient;

	public BidsController(IMapper mapper, IPublishEndpoint publishEndpoint, GrpAuctionClient grpcClient)
	{
		_mapper = mapper;
		_publishEndpoint = publishEndpoint;
		_grpcClient = grpcClient;
	}

	[Authorize]
	[HttpPost]
	public async Task<ActionResult<BidDto>> PlaceBid(string auctionId, int amount)
	{
		var auction = await DB.Find<Auction>().OneAsync(auctionId);

		if (auction == null)
		{
			auction = _grpcClient.GetAuction(auctionId);

			if (auction == null)
			{
				return BadRequest("Cannot accept bids on that auctio at this time");
			}
		}

		if (auction.Seller == User.Identity.Name)
		{
			return BadRequest("You cannot bid on your own auction");
		}

		var bid = new Bid
		{
			Amount = amount,
			AuctionId = auctionId,
			Bidder = User.Identity.Name
		};

		if (auction.AuctionEnd < DateTime.UtcNow)
		{
			bid.BidStatus = BidStatus.Finished;
		}
		else
		{
			var highBid = await DB.Find<Bid>().Match(a => a.AuctionId == auctionId).Sort(b => b.Descending(x => x.Amount)).ExecuteFirstAsync();

			if (highBid != null && amount > highBid.Amount || highBid == null)
			{
				bid.BidStatus = amount > auction.ReservcePrice ? BidStatus.Accepted : BidStatus.AcceptedBellowReserve;
			}

			if (highBid != null && bid.Amount <= highBid.Amount)
			{
				bid.BidStatus = BidStatus.TooLow;
			}
		}

		await DB.SaveAsync(bid);
		await _publishEndpoint.Publish(_mapper.Map<BidPlaced>(bid));

		return Ok(_mapper.Map<BidDto>(bid));
	}

	[HttpGet("{auctionId}")]
	public async Task<ActionResult<IEnumerable<BidDto>>> GetBidsForAuction([FromRoute] string auctionId)
	{
		var bids = await DB.Find<Bid>().Match(ss => ss.AuctionId == auctionId).Sort(s => s.Descending(x => x.BidTime)).ExecuteAsync();

		return bids.Select(_mapper.Map<BidDto>).ToList();
	}
}