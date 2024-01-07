using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;
public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;

    public AuctionUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }
    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        var item = _mapper.Map<Item>(context.Message);

        await DB.Update<Item>()
            .MatchID(item.ID)
            .Modify(a => a.Make, item.Make)
            .Modify(a => a.Model, item.Model)
            .Modify(a => a.Year, item.Year)
            .Modify(a => a.Color, item.Color)
            .Modify(a => a.Mileage, item.Mileage)
            .ExecuteAsync();
    }
}
