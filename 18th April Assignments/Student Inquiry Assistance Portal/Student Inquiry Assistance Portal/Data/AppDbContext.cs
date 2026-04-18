using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Student_Inquiry_Assistance_Portal.Models;


namespace Student_Inquiry_Assistance_Portal.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enquiry> Enquiries { get; set; }
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            SeedRoles(modelBuilder);

            modelBuilder.Entity<Payment>()
        .HasOne(p => p.Student)
        .WithMany()
        .HasForeignKey(p => p.StudentId)
        .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Admission)
                .WithMany(a => a.Payments)
                .HasForeignKey(p => p.AdmissionID)
                .OnDelete(DeleteBehavior.NoAction);

        }
        private static void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData
            (
            new IdentityRole()
            {
                Name = "Admin",
                ConcurrencyStamp = "1",
                NormalizedName = "ADMIN"
            },
            new IdentityRole()
            {
                Name = "Student",
                ConcurrencyStamp = "2",
                NormalizedName = "STUDENT"
            },
            new IdentityRole()
            {
                Name = "OfficeStaff",
                ConcurrencyStamp = "3",
                NormalizedName = "OFFICESTAFF"
            }

            );
        }
    }
}
