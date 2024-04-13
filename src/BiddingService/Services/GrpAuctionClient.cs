using AuctionService;
using BiddingService.Models;
using Grpc.Net.Client;

namespace BiddingService.Services;

public class GrpAuctionClient
{
	private readonly ILogger<GrpAuctionClient> _logger;
	private readonly IConfiguration _config;

	public GrpAuctionClient(ILogger<GrpAuctionClient> logger, IConfiguration config)
	{
		_logger = logger;
		_config = config;
	}

	public Auction GetAuction(string id)
	{
		_logger.LogInformation("Calling gRPC Service");

		var channel = GrpcChannel.ForAddress(_config["GrpcAuction"]);
		var client = new GrpcAuction.GrpcAuctionClient(channel);
		var request = new GetAuctionRequest { Id = id };

		try
		{
			var reply = client.GetAuction(request);
			var auction = new Auction
			{
				ID = reply.Auction.Id,
				AuctionEnd = DateTime.Parse(reply.Auction.AuctionEnd),
				Seller = reply.Auction.Seller,
				ReservcePrice = reply.Auction.ReservePrice,
			};

			return auction;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Could not call gRPC Server");
			return null;
		}
	}
}
