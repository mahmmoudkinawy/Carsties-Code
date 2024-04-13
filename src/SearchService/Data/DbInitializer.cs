using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Data;

public class DbInitializer
{
	public static async Task InitDb(WebApplication app)
	{
		await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

		await DB.Index<Item>().Key(i => i.Make, KeyType.Text).Key(i => i.Model, KeyType.Text).Key(i => i.Color, KeyType.Text).CreateAsync();

		if (await DB.CountAsync<Item>() == 0)
		{
			var auctions = await File.ReadAllTextAsync("Data/auctions.json");

			var items = JsonSerializer.Deserialize<IReadOnlyList<Item>>(auctions, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			await items.SaveAsync();
		}
	}
}
