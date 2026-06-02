using BloodDonationApp.Models;
using System;
using System.Linq;

namespace BloodDonationApp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, bool isDevelopment = false)
        {
            // Force recreation of database during development to ensure seeding runs cleanly
            if (isDevelopment)
            {
                context.Database.EnsureDeleted();
            }
            context.Database.EnsureCreated();

            // Look for any donors.
            if (context.Donors.Any())
            {
                return;   // DB has been seeded
            }

            string defaultHashedPassword = PasswordHasher.HashPassword("Password123!");

            var donors = new Donor[]
            {
                new Donor { Name = "Shravani", BloodType = "O+", Phone = "9876543210", Email = "shravani123@gmail.com", City = "pune ", LastDonationDate = DateTime.Parse("2024-01-01"), Password = defaultHashedPassword, ConfirmPassword = "Password123!", Age = 25, Gender = "Female", Address = "123 Ganeshkhind Road", State = "Maharashtra", PinCode = "411007", IsVerified = true, TotalDonations = 3, Latitude = 18.5385, Longitude = 73.8340, VerificationToken = "shravani-token-guid-12345" },
                new Donor { Name = "Nikita", BloodType = "A-", Phone = "9876543211", Email = "nikita123@gmail.com", City = "mumbai", LastDonationDate = DateTime.Parse("2023-11-15"), Password = defaultHashedPassword, ConfirmPassword = "Password123!", Age = 30, Gender = "Female", Address = "456 Marine Drive", State = "Maharashtra", PinCode = "400002", IsVerified = true, TotalDonations = 5, Latitude = 18.9436, Longitude = 72.8232, VerificationToken = "nikita-token-guid-12345" },
                new Donor { Name = "Snehal", BloodType = "B+", Phone = "8876543212", Email = "snehal123@gmail.com", City = "pune ", IsAvailable = false, Password = defaultHashedPassword, ConfirmPassword = "Password123!", Age = 22, Gender = "Female", Address = "789 Kothrud Chowk", State = "Maharashtra", PinCode = "411038", IsVerified = false, TotalDonations = 0, Latitude = 18.5074, Longitude = 73.8077, VerificationToken = "snehal-token-guid-12345" },
                new Donor { Name = "Bharat", BloodType = "AB+", Phone = "7876543213", Email = "bharat123@gmail.com", City = "goa", Password = defaultHashedPassword, ConfirmPassword = "Password123!", Age = 28, Gender = "Male", Address = "101 Panaji Beach Road", State = "Goa", PinCode = "403001", IsVerified = true, TotalDonations = 1, Latitude = 15.4989, Longitude = 73.8278, VerificationToken = "bharat-token-guid-12345" }
            };

            foreach (Donor d in donors)
            {
                context.Donors.Add(d);
            }
            context.SaveChanges();

            // Retrieve donors to get their real IDs
            var shravani = context.Donors.First(d => d.Name == "Shravani");
            var nikita = context.Donors.First(d => d.Name == "Nikita");
            var snehal = context.Donors.First(d => d.Name == "Snehal");
            var bharat = context.Donors.First(d => d.Name == "Bharat");

            var donationRecords = new DonationRecord[]
            {
                // Shravani (3 completed donations, unlocks First Drop & Life Saver badges)
                new DonationRecord { DonorId = shravani.Id, DonationDate = DateTime.Now.AddMonths(-1), Units = 1, BloodType = shravani.BloodType, Hospital = "City Red Cross Clinic", City = shravani.City, Notes = "Regular donation", Status = "Completed" },
                new DonationRecord { DonorId = shravani.Id, DonationDate = DateTime.Now.AddMonths(-5), Units = 1, BloodType = shravani.BloodType, Hospital = "General City Hospital", City = shravani.City, Notes = "Emergency donation response", Status = "Completed" },
                new DonationRecord { DonorId = shravani.Id, DonationDate = DateTime.Now.AddMonths(-9), Units = 1, BloodType = shravani.BloodType, Hospital = "Donora Blood Drive", City = shravani.City, Notes = "Regular checkup donation", Status = "Completed" },

                // Nikita (5 completed donations, including an emergency note. Unlocks First Drop, Life Saver, Hero of Donora, and Emergency Responder badges)
                new DonationRecord { DonorId = nikita.Id, DonationDate = DateTime.Now.AddDays(-10), Units = 2, BloodType = nikita.BloodType, Hospital = "Ruby Hall Clinic", City = nikita.City, Notes = "Emergency request responder", Status = "Completed" },
                new DonationRecord { DonorId = nikita.Id, DonationDate = DateTime.Now.AddMonths(-4), Units = 1, BloodType = nikita.BloodType, Hospital = "Jehangir Hospital", City = nikita.City, Notes = "Regular donation", Status = "Completed" },
                new DonationRecord { DonorId = nikita.Id, DonationDate = DateTime.Now.AddMonths(-8), Units = 1, BloodType = nikita.BloodType, Hospital = "Sassoon Hospital", City = nikita.City, Notes = "Regular donation", Status = "Completed" },
                new DonationRecord { DonorId = nikita.Id, DonationDate = DateTime.Now.AddMonths(-12), Units = 1, BloodType = nikita.BloodType, Hospital = "KEM Hospital", City = nikita.City, Notes = "First donation", Status = "Completed" },
                new DonationRecord { DonorId = nikita.Id, DonationDate = DateTime.Now.AddMonths(-16), Units = 1, BloodType = nikita.BloodType, Hospital = "Blood Bank Main", City = nikita.City, Notes = "First donation", Status = "Completed" },
                new DonationRecord { DonorId = nikita.Id, DonationDate = DateTime.Now.AddDays(2), Units = 1, BloodType = nikita.BloodType, Hospital = "Future Clinic", City = nikita.City, Notes = "Pending appointment", Status = "Pending" },

                // Snehal (0 completed donations, but 1 pending and 1 cancelled. No badges unlocked)
                new DonationRecord { DonorId = snehal.Id, DonationDate = DateTime.Now.AddDays(-5), Units = 1, BloodType = snehal.BloodType, Hospital = "Pune Blood Center", City = snehal.City, Notes = "Donor felt unwell during check", Status = "Cancelled" },
                new DonationRecord { DonorId = snehal.Id, DonationDate = DateTime.Now.AddDays(5), Units = 1, BloodType = snehal.BloodType, Hospital = "Sahyadri Hospital", City = snehal.City, Notes = "Scheduled donation", Status = "Pending" },

                // Bharat (1 completed donation, unlocks First Drop)
                new DonationRecord { DonorId = bharat.Id, DonationDate = DateTime.Now.AddMonths(-2), Units = 1, BloodType = bharat.BloodType, Hospital = "Goa Medical College", City = bharat.City, Notes = "Regular donation", Status = "Completed" }
            };

            foreach (DonationRecord dr in donationRecords)
            {
                context.DonationRecords.Add(dr);
            }
            context.SaveChanges();

            var requests = new System.Collections.Generic.List<BloodRequest>
            {
                new BloodRequest { PatientName = "shreyshi", BloodType = "O+", Units = 2, Hospital = "City Hospital", City = "pune ", ContactNumber = "9876543210", RequiredDate = DateTime.Now.AddDays(2), Status = "Pending", RequesterName = "Family Member", UrgencyLevel = UrgencyLevel.Normal, CreatedAt = DateTime.UtcNow, Latitude = 18.5204, Longitude = 73.8567 },
                new BloodRequest { PatientName = "ayush ", BloodType = "A-", Units = 1, Hospital = "General Clinic", City = " mumbai", ContactNumber = "9876543211", RequiredDate = DateTime.Now.AddDays(1), Status = "Pending", RequesterName = "Doctor Office", UrgencyLevel = UrgencyLevel.Urgent, CreatedAt = DateTime.UtcNow, Latitude = 19.0760, Longitude = 72.8777 }
            };

            foreach (BloodRequest r in requests)
            {
                context.BloodRequests.Add(r);
            }
            context.SaveChanges();

            // Seed rich history of blood requests for ML analytics
            var rand = new Random(42); // Seeded for reproducibility
            string[] bloodTypes = { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
            string[] cities = { "Pune", "Mumbai", "Goa" };
            string[] hospitals = { "City Hospital", "General Clinic", "Ruby Hall Clinic", "Sassoon Hospital", "KEM Hospital", "City Red Cross Clinic" };
            string[] urgencies = { "Normal", "Urgent", "Critical" };

            // Seed requests for the past 180 days
            for (int i = 180; i >= 1; i--)
            {
                // Generate 0 to 3 requests per day
                int requestsCount = rand.Next(0, 4);
                for (int j = 0; j < requestsCount; j++)
                {
                    var createdDate = DateTime.Today.AddDays(-i).AddHours(rand.Next(8, 20));
                    var bloodType = bloodTypes[rand.Next(bloodTypes.Length)];
                    var units = rand.Next(1, 6);
                    // Set historical status mostly to Fulfilled, others to Pending/Cancelled
                    var statusRand = rand.Next(10);
                    var status = statusRand < 7 ? "Fulfilled" : (statusRand < 9 ? "Pending" : "Cancelled");

                    var req = new BloodRequest
                    {
                        PatientName = $"Patient_{i}_{j}",
                        BloodType = bloodType,
                        Units = units,
                        Hospital = hospitals[rand.Next(hospitals.Length)],
                        City = cities[rand.Next(cities.Length)],
                        ContactNumber = $"98765432{rand.Next(10, 99)}",
                        RequiredDate = createdDate.AddDays(rand.Next(1, 4)),
                        Status = status,
                        RequesterName = $"Requester_{i}_{j}",
                        UrgencyLevel = (UrgencyLevel)rand.Next(3),
                        CreatedAt = createdDate,
                        Latitude = 18.5204 + (rand.NextDouble() - 0.5) * 0.1,
                        Longitude = 73.8567 + (rand.NextDouble() - 0.5) * 0.1
                    };
                    context.BloodRequests.Add(req);
                }
            }
            context.SaveChanges();

            // Seed Blood Inventory
            var inventory = new BloodInventory[]
            {
                new BloodInventory { BloodType = "O+", UnitsAvailable = 15, UnitsReserved = 2, LastUpdated = DateTime.Now },
                new BloodInventory { BloodType = "O-", UnitsAvailable = 8, UnitsReserved = 1, LastUpdated = DateTime.Now },
                new BloodInventory { BloodType = "A+", UnitsAvailable = 12, UnitsReserved = 0, LastUpdated = DateTime.Now },
                new BloodInventory { BloodType = "A-", UnitsAvailable = 5, UnitsReserved = 0, LastUpdated = DateTime.Now },
                new BloodInventory { BloodType = "B+", UnitsAvailable = 20, UnitsReserved = 3, LastUpdated = DateTime.Now },
                new BloodInventory { BloodType = "B-", UnitsAvailable = 6, UnitsReserved = 1, LastUpdated = DateTime.Now },
                new BloodInventory { BloodType = "AB+", UnitsAvailable = 10, UnitsReserved = 2, LastUpdated = DateTime.Now },
                new BloodInventory { BloodType = "AB-", UnitsAvailable = 4, UnitsReserved = 0, LastUpdated = DateTime.Now }
            };

            foreach (var inv in inventory)
            {
                context.BloodInventory.Add(inv);
            }

            // Seed Blood Camps
            var camps = new BloodCamp[]
            {
                new BloodCamp
                {
                    CampName = "Mega Blood Donation Camp Pune",
                    OrganizedBy = "City Red Cross Society",
                    Location = "Bal Gandharva Rang Mandir, Shivaji Nagar",
                    City = "Pune",
                    State = "Maharashtra",
                    ScheduledDate = DateTime.Now.AddDays(7),
                    MaxDonors = 200,
                    RegisteredCount = 45,
                    IsActive = true,
                    ContactEmail = "pune.redcross@example.com",
                    ContactPhone = "02025531234",
                    Latitude = 18.5222,
                    Longitude = 73.8488
                },
                new BloodCamp
                {
                    CampName = "Rotary Club Lifesaver Drive",
                    OrganizedBy = "Rotary Club Mumbai West",
                    Location = "YMCA Ground, Mumbai Central",
                    City = "Mumbai",
                    State = "Maharashtra",
                    ScheduledDate = DateTime.Now.AddDays(14),
                    MaxDonors = 150,
                    RegisteredCount = 12,
                    IsActive = true,
                    ContactEmail = "mumbairotary@example.com",
                    ContactPhone = "02223085678",
                    Latitude = 18.9696,
                    Longitude = 72.8194
                }
            };

            foreach (var camp in camps)
            {
                context.BloodCamps.Add(camp);
            }
            context.SaveChanges();

            // Seed Admins
            var admins = new Admin[]
            {
                new Admin
                {
                    Name = "Super Administrator",
                    Email = "admin1@blood.org",
                    Password = PasswordHasher.HashPassword("admin123"),
                    Role = "SuperAdmin",
                    CreatedAt = DateTime.Now
                },
                new Admin
                {
                    Name = "Camp Administrator",
                    Email = "admin2@blood.org",
                    Password = PasswordHasher.HashPassword("admin123"),
                    Role = "Admin",
                    CreatedAt = DateTime.Now
                }
            };

            foreach (var admin in admins)
            {
                context.Admins.Add(admin);
            }
            context.SaveChanges();

            // Seed Notifications for donors
            var notifications = new Notification[]
            {
                new Notification { DonorId = shravani.Id, Title = "Welcome to Donora", Message = "Thank you for registering. You have been verified as a donor!", Type = "Success", IsRead = false, CreatedAt = DateTime.Now.AddDays(-2) },
                new Notification { DonorId = shravani.Id, Title = "Urgent Request", Message = "An urgent blood request has been posted for O+ in Pune.", Type = "Warning", IsRead = false, CreatedAt = DateTime.Now.AddDays(-1) },
                new Notification { DonorId = nikita.Id, Title = "Profile Verified", Message = "Your profile has been successfully verified by an administrator.", Type = "Success", IsRead = false, CreatedAt = DateTime.Now.AddDays(-3) },
                new Notification { DonorId = nikita.Id, Title = "Stock Update", Message = "Blood stock for A- is currently running low.", Type = "Info", IsRead = true, CreatedAt = DateTime.Now.AddDays(-5) },
                new Notification { DonorId = snehal.Id, Title = "Verification Pending", Message = "Your profile is pending verification. Please verify your phone number.", Type = "Info", IsRead = false, CreatedAt = DateTime.Now.AddDays(-1) },
                new Notification { DonorId = bharat.Id, Title = "Camp Alert", Message = "A new blood donation camp is scheduled near you in Goa.", Type = "Info", IsRead = false, CreatedAt = DateTime.Now.AddDays(-4) }
            };

            foreach (var n in notifications)
            {
                context.Notifications.Add(n);
            }
            context.SaveChanges();

            // Seed Badges
            var badges = new Badge[]
            {
                new Badge { Name = "First Drop", Description = "Completed your first blood donation!", Icon = "bi-droplet-fill", ColorHex = "#FF4D4D", Criteria = "donations:1", PointsAwarded = 100 },
                new Badge { Name = "Life Saver", Description = "Completed 3 blood donations!", Icon = "bi-heart-fill", ColorHex = "#FF2E93", Criteria = "donations:3", PointsAwarded = 150 },
                new Badge { Name = "Hero of Donora", Description = "Completed 5 blood donations!", Icon = "bi-shield-fill-check", ColorHex = "#D4AF37", Criteria = "donations:5", PointsAwarded = 250 },
                new Badge { Name = "Emergency Responder", Description = "Responded to an emergency blood request!", Icon = "bi-lightning-fill", ColorHex = "#FF9F1C", Criteria = "emergency:1", PointsAwarded = 150 },
                new Badge { Name = "Loyal Donor", Description = "Reached Level 3 (Silver Guardian)!", Icon = "bi-award-fill", ColorHex = "#9B5DE5", Criteria = "level:3", PointsAwarded = 200 }
            };
            foreach (var b in badges)
            {
                context.Badges.Add(b);
            }
            context.SaveChanges();

            // Seed Rewards
            var rewards = new Reward[]
            {
                new Reward { Name = "Blood Donor T-Shirt", Description = "A premium cotton red t-shirt with 'Blood Donor' emblem.", PointsCost = 300, Icon = "bi-tag-fill", AvailableQuantity = 50 },
                new Reward { Name = "First Aid Kit", Description = "A compact, complete emergency first aid kit.", PointsCost = 500, Icon = "bi-file-medical-fill", AvailableQuantity = 30 },
                new Reward { Name = "Health Check Voucher", Description = "Free full body health checkup at partnering clinics.", PointsCost = 200, Icon = "bi-heart-pulse-fill", AvailableQuantity = 100 },
                new Reward { Name = "Donor Certificate Frame", Description = "A high-quality wooden certificate frame.", PointsCost = 100, Icon = "bi-mortarboard-fill", AvailableQuantity = 75 }
            };
            foreach (var r in rewards)
            {
                context.Rewards.Add(r);
            }
            context.SaveChanges();

            // Seed levels, points on Donors
            shravani.RewardPoints = 950;
            shravani.LifetimePoints = 950;
            shravani.Level = 4;
            shravani.LevelName = "Gold Lifesaver";

            nikita.RewardPoints = 1950;
            nikita.LifetimePoints = 1950;
            nikita.Level = 5;
            nikita.LevelName = "Legendary Hero";

            snehal.RewardPoints = 100;
            snehal.LifetimePoints = 100;
            snehal.Level = 2;
            snehal.LevelName = "Bronze Savior";

            bharat.RewardPoints = 600;
            bharat.LifetimePoints = 600;
            bharat.Level = 4;
            bharat.LevelName = "Gold Lifesaver";

            context.SaveChanges();

            // Seed DonorBadges
            var bFirstDrop = context.Badges.First(b => b.Name == "First Drop");
            var bLifeSaver = context.Badges.First(b => b.Name == "Life Saver");
            var bHero = context.Badges.First(b => b.Name == "Hero of Donora");
            var bEmergency = context.Badges.First(b => b.Name == "Emergency Responder");
            var bLoyal = context.Badges.First(b => b.Name == "Loyal Donor");

            context.DonorBadges.AddRange(new DonorBadge[]
            {
                new DonorBadge { DonorId = shravani.Id, BadgeId = bFirstDrop.Id, UnlockedAt = DateTime.Now.AddMonths(-9) },
                new DonorBadge { DonorId = shravani.Id, BadgeId = bLifeSaver.Id, UnlockedAt = DateTime.Now.AddMonths(-1) },
                new DonorBadge { DonorId = nikita.Id, BadgeId = bFirstDrop.Id, UnlockedAt = DateTime.Now.AddMonths(-16) },
                new DonorBadge { DonorId = nikita.Id, BadgeId = bLifeSaver.Id, UnlockedAt = DateTime.Now.AddMonths(-8) },
                new DonorBadge { DonorId = nikita.Id, BadgeId = bHero.Id, UnlockedAt = DateTime.Now.AddDays(-10) },
                new DonorBadge { DonorId = nikita.Id, BadgeId = bEmergency.Id, UnlockedAt = DateTime.Now.AddDays(-10) },
                new DonorBadge { DonorId = nikita.Id, BadgeId = bLoyal.Id, UnlockedAt = DateTime.Now.AddMonths(-4) },
                new DonorBadge { DonorId = bharat.Id, BadgeId = bFirstDrop.Id, UnlockedAt = DateTime.Now.AddMonths(-2) },
                new DonorBadge { DonorId = bharat.Id, BadgeId = bLoyal.Id, UnlockedAt = DateTime.Now.AddMonths(-2) }
            });
            context.SaveChanges();
        }
    }
}
