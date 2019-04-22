using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Betty.Database;
using Betty.Services;
using Betty.Utilities;
using Betty.Utilities.DateTimeUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TimeZoneConverter;

namespace Betty.NUnitIntegrationTest.Services
{
    public class AgendaTest
    {
        [Test, Description("Test if the constructor removes all singular appointments that have taken place in the past.")]
        public void Constructor_OnceAppointmentInThePast_AppointmentIsRemovedFromDatabase()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0);
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .BuildServiceProvider();
                using(var database = new BettyDB(options))
                {
                    database.Appointments.Add(new Appointment
                    {
                        Date = dateTimeProvider.UtcNow.AddDays(-2),
                        Timezone = TimeZoneInfo.Utc,
                        Repetition = Repetition.FromId("o"),
                        Title = "My past singular appointment",
                    });
                    database.SaveChanges();
                }

                // act
                Agenda agenda = new Agenda(services);

                // assert
                using(var database = new BettyDB(options))
                {
                    var dbresult = from app in database.Appointments
                                   select app;
                    Assert.AreEqual(0, dbresult.Count());
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if the constructor updates all repetitive appointments that have taken place in the past.")]
        public void Constructor_RepetitiveAppointmentInThePast_AppointmentIsUpdated()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0);
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .BuildServiceProvider();
                using (var database = new BettyDB(options))
                {
                    database.Appointments.Add(new Appointment
                    {
                        Date = dateTimeProvider.UtcNow.AddHours(-2),
                        Timezone = TimeZoneInfo.Utc,
                        Repetition = Repetition.FromId("d1"),
                        Title = "My past singular appointment",
                    });
                    database.SaveChanges();
                }

                // act
                Agenda agenda = new Agenda(services);

                // assert
                using (var database = new BettyDB(options))
                {
                    var dbresult = (from app in database.Appointments
                                   select app).First();
                    Assert.AreEqual(dateTimeProvider.UtcNow.AddHours(-2).AddDays(1), dbresult.Date);
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if the 'Plan' method adds the appointment to the database if it is valid.")]
        public async Task PlanAsync_CorrectAppointment_DatabaseContainsAppointment()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    await database.Database.EnsureCreatedAsync();
                }

                // arrange
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                AppointmentNotification[] notifications = new AppointmentNotification[]
                {
                    new AppointmentNotification
                    {
                        Offset = TimeSpan.FromMinutes(30)
                    }
                };
                Appointment appointment = new Appointment
                {
                    Date = DateTime.UtcNow.AddDays(2),
                    Repetition = Repetition.FromId("o"),
                    Title = "My Appointment",
                    Timezone = TimeZoneInfo.Utc,
                    Notifications = notifications
                };

                // act
                var result = await agenda.PlanAsync(appointment);

                // assert
                Assert.AreEqual(MethodResult.success, result);
                using (var database = new BettyDB(options))
                {
                    Appointment a = (from app in database.Appointments
                                     select app).FirstOrDefault();
                    Assert.AreEqual(appointment.Id, a.Id);
                    AppointmentNotification n = (from app in database.AppointmentNotifications
                                                 select app).FirstOrDefault();
                    Assert.AreEqual(notifications[0].Id, n.Id);
                }
            }
            finally
            {
                // close database connection
                connection.Close();
            }
        }

        [Test, Description("Test if the 'Plan' method throws an exception when the provided appointment is incomplete.")]
        public async Task PlanAsync_InvalidAppointment_DatabaseDoesNotContainAppointment()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    await database.Database.EnsureCreatedAsync();
                }

                // arrange
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                Appointment appointment = new Appointment
                {
                    Date = DateTime.UtcNow.AddDays(2),
                    Timezone = TimeZoneInfo.Utc,
                    Repetition = Repetition.FromId("o")
                };

                // act
                var result = await agenda.PlanAsync(appointment);

                // assert
                Assert.AreNotEqual(MethodResult.success, result);
                using (var database = new BettyDB(options))
                {
                    var dbresult = from app in database.Appointments
                                   select app;
                    Assert.AreEqual(0, dbresult.Count());
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if the 'Cancel' method removes an appointment from the database when a valid id is provided.")]
        public async Task CancelAsync_AppointmentExists_EventIsRemovedFromDatabase()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    await database.Database.EnsureCreatedAsync();
                }

                // arrange
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                AppointmentNotification[] notifications = new AppointmentNotification[]
                {
                    new AppointmentNotification
                    {
                        Offset = new TimeSpan(1, 0, 0),
                    }
                };
                Appointment appointment = new Appointment
                {
                    Date = DateTime.UtcNow.AddDays(2),
                    Timezone = TimeZoneInfo.Utc,
                    Repetition = Repetition.FromId("o"),
                    Title = "My cancelled appointment",
                    Notifications = notifications
                };
                using (var database = new BettyDB(options))
                {
                    database.Appointments.Add(appointment);
                    await database.SaveChangesAsync();
                }

                // act
                var result = await agenda.CancelAsync(appointment.Id);

                // assert
                Assert.AreEqual(MethodResult.success, result);
                using(var database = new BettyDB(options))
                {
                    var dbresult = from app in database.Appointments
                                   select app;
                    Assert.AreEqual(0, dbresult.Count());
                    var dbresultnot = from appnot in database.AppointmentNotifications
                                      select appnot;
                    Assert.AreEqual(0, dbresultnot.Count());
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if 'Cancel' method returns a 'not found' result when appointment with given id does not exist.")]
        public async Task CancelAsync_AppointmentDoesNotExist_ReturnsMethodResultnotfound()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    await database.Database.EnsureCreatedAsync();
                }

                // arrange
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                Appointment appointment = new Appointment
                {
                    Date = DateTime.UtcNow.AddDays(2),
                    Timezone = TimeZoneInfo.Utc,
                    Repetition = Repetition.FromId("o"),
                    Title = "My cancelled appointment"
                };
                using (var database = new BettyDB(options))
                {
                    database.Appointments.Add(appointment);
                    await database.SaveChangesAsync();
                }

                // act
                var result = await agenda.CancelAsync(appointment.Id + 1);

                // assert
                Assert.AreEqual(MethodResult.notfound, result);
                using (var database = new BettyDB(options))
                {
                    var dbresult = from app in database.Appointments
                                   select app;
                    Assert.AreEqual(1, dbresult.Count());
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if 'GetNextNotificationDate' returns the correct date if earliest date is from notification.")]
        public void GetNearestNotificationDate_ClosestDateIsNotification_ReturnsCorrectDateTime()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                AppointmentNotification[] notifications = new AppointmentNotification[]
                {
                    new AppointmentNotification
                    {
                        Offset = TimeSpan.FromHours(2)
                    }
                };
                Appointment appointment = new Appointment
                {
                    Date = dateTimeProvider.UtcNow.AddHours(3),
                    Repetition = Repetition.FromId("o"),
                    Title = "My Appointment",
                    Timezone = TimeZoneInfo.Utc,
                    Notifications = notifications
                };
                using(var database = new BettyDB(options))
                {
                    database.Appointments.Add(appointment);
                    database.SaveChanges();
                }

                // act
                DateTime? result = agenda.GetNearestNotificationDate(dateTimeProvider.UtcNow);

                // assert
                Assert.AreEqual(dateTimeProvider.UtcNow.AddHours(1), result.Value);
                agenda.Dispose();
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if 'GetNextNotificationDate' returns the correct date if earliest date is from appointment.")]
        public void GetNearestNotificationDate_ClosestDateIsAppointment_ReturnsCorrectDateTime()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                AppointmentNotification[] notifications = new AppointmentNotification[]
                {
                    new AppointmentNotification
                    {
                        Offset = TimeSpan.FromHours(2)
                    }
                };
                Appointment appointment = new Appointment
                {
                    Date = dateTimeProvider.UtcNow.AddHours(1),
                    Repetition = Repetition.FromId("o"),
                    Title = "My Appointment",
                    Timezone = TimeZoneInfo.Utc,
                    Notifications = notifications
                };
                using (var database = new BettyDB(options))
                {
                    database.Appointments.Add(appointment);
                    database.SaveChanges();
                }

                // act
                DateTime? result = agenda.GetNearestNotificationDate(dateTimeProvider.UtcNow);

                // assert
                Assert.AreEqual(dateTimeProvider.UtcNow.AddHours(1), result.Value);
                agenda.Dispose();
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if 'GetNextNotificationDate' returns the correct date if earliest date is from notification.")]
        public void GetNearestNotificationDate_NoAppointments_ReturnsNull()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);

                // act
                DateTime? result = agenda.GetNearestNotificationDate(dateTimeProvider.UtcNow);

                // assert
                Assert.IsNull(result);
                agenda.Dispose();
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if 'ValidateAppointment' returns success when an appointment is valid.")]
        public void ValidateAppointment_CorrectAppointment_ReturnsSuccess()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0);
                IServiceProvider services = new ServiceCollection()
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .AddTransient(x => new BettyDB(options))
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                Appointment appointment = new Appointment
                {
                    Date = dateTimeProvider.UtcNow.AddDays(1),
                    Timezone = TimeZoneInfo.Utc,
                    Title = "Test Appointment",
                    Repetition = Repetition.FromId("d1"),
                };

                // act
                MethodResult result = agenda.ValidateAppointment(appointment, dateTimeProvider.UtcNow);

                // assert
                Assert.AreEqual(MethodResult.success, result);
                agenda.Dispose();
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if the 'ValidateAppointment' method returns the correct error code if an invalid time was provided with the appointment.")]
        public void ValidateAppointment_InvalidTime_ReturnsMethodResultdateinvalid()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                Appointment appointment = new Appointment
                {
                    Date = new DateTime(2022, 3, 27, 2, 30, 0),   // this time should not exist, because the time skips from 2am to 3am 
                    Timezone = TZConvert.GetTimeZoneInfo("Central European Standard Time"),
                    Title = "dst skips this hour",
                    Repetition = Repetition.FromId("o"),
                };

                // act
                var result = agenda.ValidateAppointment(appointment, dateTimeProvider.UtcNow);

                // assert
                Assert.AreEqual(Agenda.dateinvalid, result);
            }
            finally
            {
                connection.Close();
            }
        }

        [Test, Description("Test if the 'ValidateAppointment' method returns the correct error code when a past date was provided.")]
        public void ValidateAppointment_DateInThePast_ReturnsMethodResultdatepassed()
        {
            // create database connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BettyDB>()
                    .UseSqlite(connection)
                    .Options;

                using (var database = new BettyDB(options))
                {
                    database.Database.EnsureCreated();
                }

                // arrange
                FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider();
                dateTimeProvider.UtcNow = new DateTime(2019, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .AddSingleton<IDateTimeProvider>(dateTimeProvider)
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                Appointment appointment = new Appointment
                {
                    Date = new DateTime(2018, 12, 31, 12, 0, 0),   // this time should not exist, because the time skips from 2am to 3am 
                    Timezone = TZConvert.GetTimeZoneInfo("Central European Standard Time"),
                    Title = "This event is in the past",
                    Repetition = Repetition.FromId("o"),
                };

                // act
                var result = agenda.ValidateAppointment(appointment, dateTimeProvider.UtcNow);

                // assert
                Assert.AreEqual(Agenda.datepassed, result);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
