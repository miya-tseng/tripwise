using Microsoft.EntityFrameworkCore;

namespace TripWise.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    
}