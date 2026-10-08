using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
            .HasMaxLength(User.UserNameMaxLength)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(u => u.HomePage)
            .HasMaxLength(User.HomePageMaxLength);

        // The same name + e-mail pair always means the same user.
        builder.HasIndex(u => new { u.UserName, u.Email })
            .IsUnique();
    }
}
