using Microsoft.EntityFrameworkCore;
using Routine.Api.Entities;
using System;

namespace Routine.Api.Data
{
    public class RoutineDbContext: DbContext
    {
        public RoutineDbContext(DbContextOptions<RoutineDbContext> options)
            : base(options)
        {

        }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>()
                .Property(x => x.Name).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Company>()
                .Property(x => x.Introduction).HasMaxLength(500);

            modelBuilder.Entity<Employee>()
                .Property(x => x.EmployeeNo).IsRequired().HasMaxLength(10);
            modelBuilder.Entity<Employee>()
                .Property(x => x.FirstName).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Employee>()
                .Property(x => x.LastName).IsRequired().HasMaxLength(50);

            modelBuilder.Entity<Employee>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.CompanyId)
                /* 级联删除 */
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Company>()
                .HasData(
                    new Company
                    {
                        Id = Guid.Parse("bbdee09c-089b-4d30-bece-44df5923716c"),
                        Name = "Microsoft",
                        Introduction = "Great Company"
                    },
                    new Company
                    {
                        Id = Guid.Parse("6fb600c1-9011-4fd7-9234-881379716440"),
                        Name = "Google",
                        Introduction = "Don't be evil"
                    },
                    new Company
                    {
                        Id = Guid.Parse("5efc910b-2f45-43df-afae-620d40542853"),
                        Name = "Alipapa",
                        Introduction = "Fubao Company"
                    }
                );
        }
    }
}