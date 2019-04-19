using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Betty.Database;
using Betty.Services;
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
        public void Plan_CorrectAppointment_DatabaseContainsAppointment()
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
                agenda.Plan(appointment);
                
                // assert
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
        public void Plan_MissingDataInAppointment_ThrowsArgumentException()
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
                void Result() => agenda.Plan(appointment);

                // assert
                Assert.Throws<ArgumentException>(Result);
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
        public void Plan_InvalidTime_ReturnsMethodResult102()
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
                var result = agenda.Plan(appointment);

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
        public void Plan_DateInThePast_ReturnsMethodResult101()
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
                var result = agenda.Plan(appointment);

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
        public void Cancel_AppointmentExists_EventIsRemovedFromDatabase()
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
                IServiceProvider services = new ServiceCollection()
                    .AddTransient(x => new BettyDB(options))
                    .BuildServiceProvider();
                Agenda agenda = new Agenda(services);
                using (var database = new BettyDB(options))
                {
                    database.Appointments.Add(new Appointment
                    {
                        Date = DateTime.UtcNow.AddDays(2),
                        Timezone = TimeZoneInfo.Utc,
                        Repetition = "once",
                        Title = "My cancelled appointment"
                    });
                }
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
