using AuctionService.Data;
using Grpc.Core;

namespace AuctionService.Services;

public class GrpcAuctionService : GrpcAuction.GrpcAuctionBase
{
	private readonly AuctionDbContext _dbContext;

	public GrpcAuctionService(AuctionDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public override async Task<GrpcAuctionResponse> GetAuction(GetAuctionRequest request, ServerCallContext context)
	{
		Console.WriteLine("==> Received Grpc auction request for auction");

		var auction =
			await _dbContext.Auctions.FindAsync(Guid.Parse(request.Id)) ?? throw new RpcException(new Status(StatusCode.NotFound, "Not Found"));

		var response = new GrpcAuctionResponse
		{
			Auction = new GrpcAuctionModel
			{
				AuctionEnd = auction.AuctionEnd.ToString(),
				ReservePrice = auction.ReservePrice,
				Id = auction.Id.ToString(),
				Seller = auction.Seller
			}
		};

		return response;
	}
}
