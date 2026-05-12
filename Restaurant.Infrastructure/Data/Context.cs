using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Realtima_Chat_project.Models;
using Restaurant.Core.Entity.Account.Configuration;
using Restaurant.Core.Entity.Chat;
using Restaurant.Core.Models.Account;
using System.Data;

namespace Villa_API_Project.DataAccess.Data
{
    public class Context: IdentityDbContext<ApplicationUser>
    {
        public Context()
        {
            
        }

        public Context(DbContextOptions<Context> options) : base(options)
        {

        }

        public DbSet<ApplicationUser> Users { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RevokedTokens> RevokedTokens { get; set; }

        public DbSet<Message> message { get; set; }
        public DbSet<Call> Calls { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        public DbSet<GroupMessageRead> GroupMessageReads { get; set; }
        public DbSet<GroupMessageDeleted> GroupMessageDeleteds { get; set; }
        public DbSet<MessageDeleted> MessageDeleteds { get; set; }
        public DbSet<BlockedUser> BlockedUsers { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)


        {
            optionsBuilder.UseSqlServer(@"Server=db40030.public.databaseasp.net; Database=db40030; User Id=db40030; Password=M=k6#4SwTj2_; Encrypt=False; MultipleActiveResultSets=True;");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Call>()
                .HasOne(c => c.Caller)
                .WithMany()
                .HasForeignKey(c => c.CallerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Call>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(g => g.Id);

                entity.HasOne(g => g.Creator)
                    .WithMany()
                    .HasForeignKey(g => g.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(g => g.JoinCode).IsUnique();

                entity.HasMany(g => g.Members)
                    .WithOne(m => m.Group)
                    .HasForeignKey(m => m.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(g => g.Messages)
                    .WithOne(m => m.Group)
                    .HasForeignKey(m => m.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GroupMember>(entity =>
            {
                entity.HasKey(gm => gm.Id);

                entity.HasOne(gm => gm.Group)
                    .WithMany(g => g.Members)
                    .HasForeignKey(gm => gm.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(gm => gm.User)
                    .WithMany()
                    .HasForeignKey(gm => gm.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(gm => new { gm.GroupId, gm.UserId });
            });

            modelBuilder.Entity<GroupMessage>(entity =>
            {
                entity.HasKey(gm => gm.Id);

                entity.HasOne(gm => gm.Group)
                    .WithMany(g => g.Messages)
                    .HasForeignKey(gm => gm.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(gm => gm.Sender)
                    .WithMany()
                    .HasForeignKey(gm => gm.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(gm => gm.ReadBy)
                    .WithOne(r => r.Message)
                    .HasForeignKey(r => r.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(gm => gm.CreatedAt);

                entity.HasOne(gm => gm.ReplyToMessage)
    .WithMany()
    .HasForeignKey(gm => gm.ReplyToMessageId)
    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GroupMessageRead>(entity =>
            {
                entity.HasKey(gmr => gmr.Id);

                entity.HasOne(gmr => gmr.Message)
                    .WithMany(m => m.ReadBy)
                    .HasForeignKey(gmr => gmr.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(gmr => gmr.User)
                    .WithMany()
                    .HasForeignKey(gmr => gmr.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(gmr => new { gmr.MessageId, gmr.UserId }).IsUnique();


            });

            modelBuilder.Entity<GroupMessageDeleted>(entity =>
            {
                entity.HasKey(gmd => gmd.Id);

                entity.HasOne(gmd => gmd.Message)
                    .WithMany()
                    .HasForeignKey(gmd => gmd.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(gmd => gmd.User)
                    .WithMany()
                    .HasForeignKey(gmd => gmd.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(gmd => new { gmd.MessageId, gmd.UserId });

                entity.HasIndex(gmd => gmd.DeletedForEveryone);
            });


            modelBuilder.Entity<Message>(entity =>
            {

                entity.HasOne(gm => gm.Sender)
                    .WithMany()
                    .HasForeignKey(gm => gm.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);


                entity.HasOne(gm => gm.Receiver)
                    .WithMany()
                    .HasForeignKey(gm => gm.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.ReplyToMessage)
                    .WithMany()
                    .HasForeignKey(m => m.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MessageDeleted>(entity =>
            {
                entity.HasKey(md => md.Id);

                entity.HasOne(md => md.Message)
                    .WithMany()
                    .HasForeignKey(md => md.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(md => md.User)
                    .WithMany()
                    .HasForeignKey(md => md.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(md => new { md.MessageId, md.UserId });
                entity.HasIndex(md => md.DeletedForEveryone);
            });

            modelBuilder.Entity<BlockedUser>(entity =>
            {
                entity.HasKey(bu => bu.Id);

                entity.HasOne(bu => bu.Blocker)
                    .WithMany()
                    .HasForeignKey(bu => bu.BlockerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bu => bu.BlockedUserEntity)
                    .WithMany()
                    .HasForeignKey(bu => bu.BlockedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(bu => new { bu.BlockerId, bu.BlockedUserId });
                entity.HasIndex(bu => bu.IsActive);
            });

        }
    }

}
