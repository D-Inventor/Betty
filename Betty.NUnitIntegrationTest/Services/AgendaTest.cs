using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Betty.Database;
using Betty.Services;
using Betty.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TimeZoneConverter;

namespace Betty.NUnitIntegrationTest.Services
{
    public class AgendaTest
    {
        [Test, Description("Test if the 'Plan' method adds the appointment to the database if it is valid.")]
        public async Task Plan_CorrectAppointment_DatabaseContainsAppointment()
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
                    Repetition = "once",
                    Title = "My Appointment",
                    Timezone = TimeZoneInfo.Utc
                };

                // act
                var result = await agenda.PlanAsync(appointment);

                // assert
                Assert.AreEqual(MethodResult.success, result);
                using (var database = new BettyDB(options))
                {
                    Appointment a = (from app in database.Appointments
                                     select app).FirstOrDefault();
                    Assert.AreEqual(appointment, a);
                }
            }
            finally
            {
                // close database connection
                connection.Close();
            }
        }

        [Test, Description("Test if the 'Plan' method throws an exception when the provided appointment is incomplete.")]
        public async Task Plan_MissingDataInAppointment_ThrowsArgumentException()
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
                    Repetition = "once"
                };

                // act
                async Task Result() => await agenda.PlanAsync(appointment);

                // assert
                Assert.ThrowsAsync<ArgumentException>(Result);
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

        [Test, Description("Test if the 'Plan' method returns the correct error code if an invalid time was provided with the appointment.")]
        public async Task Plan_InvalidTime_ReturnsMethodResult102()
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
                    Date = new DateTime(2022, 3, 27, 2, 30, 0),   // this time should not exist, because the time skips from 2am to 3am 
                    Timezone = TZConvert.GetTimeZoneInfo("Central European Standard Time"),
                    Title = "dst skips this hour",
                    Repetition = "once",
                };

                // act
                var result = await agenda.PlanAsync(appointment);

                // assert
                Assert.AreEqual(Agenda.dateinvalid, result);
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

        [Test, Description("Test if the 'Plan' method returns the correct error code when a past date was provided.")]
        public async Task Plan_DateInThePast_ReturnsMethodResult101()
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
                    Date = new DateTime(2018, 3, 4),
                    Timezone = TimeZoneInfo.Utc,
                    Title = "My past event",
                    Repetition = "once",
                };

                // act
                var result = await agenda.PlanAsync(appointment);

                // assert
                Assert.AreEqual(Agenda.datepassed, result);
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
        public async Task Cancel_AppointmentExists_EventIsRemovedFromDatabase()
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
                    Repetition = "once",
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
        public async Task Cancel_AppointmentDoesNotExist_ReturnsMethodResult1()
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
                    Repetition = "once",
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
    }
}
